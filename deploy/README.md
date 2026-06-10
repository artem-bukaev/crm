# CRM deployment layer

This directory contains Kubernetes manifests for the MVP CRM deployment in Yandex Cloud Managed Kubernetes. Application source code is intentionally out of scope here.

## Assumptions

- WebApi is an ASP.NET Core app listening on `8080`.
- WebApp is a static frontend served by Nginx on `80`.
- PostgreSQL is external. The connection string is provided only through the `ConnectionStrings__CrmDb` key in the `crm-secrets` Secret.
- The ALB Ingress controller is installed in the cluster. Yandex Cloud currently recommends Gwin for new setups, but these manifests target the ALB Ingress controller requested for this MVP.
- HPA needs resource metrics available in the cluster.

## Build images

```sh
docker build \
  -f docker/Crm.WebApi.Dockerfile \
  -t cr.yandex/<registry-id>/crm-webapi:<tag> \
  .

docker build \
  -f docker/Crm.WebApp.Dockerfile \
  -t cr.yandex/<registry-id>/crm-webapp:<tag> \
  --build-arg WEBAPP_DIR=src/Crm.WebApp \
  --build-arg BUILD_OUTPUT=dist \
  .
```

Replace the image names in `deploy/webapi.yaml` and `deploy/webapp.yaml` before applying.

## Configure secrets

Create the namespace first:

```sh
kubectl apply -f deploy/namespace.yaml
```

Then create the database secret from your real PostgreSQL connection string:

```sh
kubectl -n crm create secret generic crm-secrets \
  --from-literal='ConnectionStrings__CrmDb=Host=<postgres-host>;Port=6432;Database=crm;Username=<user>;Password=<password>;SSL Mode=Require;' \
  --dry-run=client \
  -o yaml | kubectl apply -f -
```

`deploy/secret.template.yaml` is a template for review and CI rendering. Do not apply it with placeholder values.

## Apply base manifests

```sh
kubectl apply -f deploy/configmap.yaml
kubectl apply -f deploy/ingress-class-alb.yaml
kubectl apply -f deploy/webapi.yaml
kubectl apply -f deploy/webapp.yaml
kubectl apply -f deploy/hpa.yaml
```

For Yandex ALB, backend Services referenced by Ingress are `NodePort`. This template reserves `30080` for WebApp and `30081` for WebApi; change them if your cluster already uses those ports.

## Apply ingress

Edit `deploy/ingress.example.yaml` first:

- set `crm.example.com`;
- set Yandex Cloud subnet IDs in `ingress.alb.yc.io/subnets`;
- set security group IDs in `ingress.alb.yc.io/security-groups`;
- set `secretName` to `yc-certmgr-cert-id-<certificate-id>` or to a Kubernetes TLS secret accepted by your ALB setup;
- set `ingress.alb.yc.io/external-ipv4-address` to `auto` or a reserved static address.

Then apply:

```sh
kubectl apply -f deploy/ingress.example.yaml
```

The WebApi deployment expects `/health/ready` and `/health/live` endpoints. If the application exposes different health endpoints, update `deploy/webapi.yaml` and the WebApi Service ALB health check annotation together.

## References

- Yandex Cloud ALB Ingress resource fields and annotations: https://yandex.cloud/en/docs/application-load-balancer/k8s-ref/ingress
- Yandex Cloud Service requirements for ALB Ingress backends: https://yandex.cloud/en/docs/application-load-balancer/k8s-ref/service-for-ingress
- Yandex Cloud IngressClass controller values: https://yandex.cloud/en/docs/managed-kubernetes/alb-ref/ingress-class
