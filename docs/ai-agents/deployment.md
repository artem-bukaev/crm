# Deployment Notes

## Target

The MVP is prepared for deployment to Yandex Cloud Managed Kubernetes.

Deployment artifacts:

- Dockerfiles in `docker/`
- Kubernetes manifests in `deploy/`
- Deployment guide in `deploy/README.md`

## Docker Images

Build API image:

```bash
docker build -f docker/Crm.WebApi.Dockerfile -t cr.yandex/<registry-id>/crm-webapi:<tag> .
```

Build WebApp image:

```bash
docker build -f docker/Crm.WebApp.Dockerfile -t cr.yandex/<registry-id>/crm-webapp:<tag> .
```

## Kubernetes Manifests

Main manifests:

- `deploy/namespace.yaml`
- `deploy/configmap.yaml`
- `deploy/secret.template.yaml`
- `deploy/webapi.yaml`
- `deploy/webapp.yaml`
- `deploy/hpa.yaml`
- `deploy/ingress-class-alb.yaml`
- `deploy/ingress.example.yaml`

Before applying manifests:

- Replace image names in `deploy/webapi.yaml` and `deploy/webapp.yaml`.
- Create `crm-secrets` with `ConnectionStrings__CrmDb`.
- Configure ingress host, subnet IDs, security group IDs, TLS secret or static IP.
- Confirm Kubernetes namespace is correct.

## Configuration

Important environment/configuration keys:

- `ConnectionStrings__CrmDb`
- `Cors__AllowedOrigins`
- `Database__ApplyMigrationsOnStartup`
- frontend API base URL during image build or runtime, depending on deployment approach

Do not commit real secrets. Use Kubernetes Secrets, Yandex Lockbox integration, CI secrets or another secret manager.

## Health Probes

WebApi exposes:

- `/health/live`
- `/health/ready`

Kubernetes probes should use these endpoints.

## Hangfire in Kubernetes

Current WebApi starts a Hangfire server in the API process.

This is acceptable for the MVP. With multiple API replicas, Hangfire can coordinate through PostgreSQL storage. If jobs become heavy or need independent scaling, move workers into a separate Kubernetes Deployment.

Hangfire Dashboard is development-only in the current host setup. Do not expose it publicly without authentication.

## Migrations

For production, prefer one of:

- CI/CD migration step before rolling deployment;
- Kubernetes Job that runs migrations;
- manually controlled migration process for sensitive releases.

Avoid relying on every API pod applying migrations on startup in production.

## Release Checklist

- Images built and pushed to Yandex Container Registry.
- Manifests point to immutable image tags.
- `crm-secrets` exists and contains the production connection string.
- CORS origins match frontend public host.
- Ingress annotations are configured for Yandex ALB.
- Health probes pass.
- API logs do not expose secrets.
- Database migration plan is explicit.
