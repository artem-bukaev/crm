import { useEffect, useMemo, useState, type Key, type ReactNode } from 'react';
import {
  CheckOutlined,
  CloseOutlined,
  DashboardOutlined,
  DeleteOutlined,
  EditOutlined,
  EyeOutlined,
  FilterOutlined,
  LockOutlined,
  LogoutOutlined,
  MessageOutlined,
  PlusOutlined,
  RobotOutlined,
  UserOutlined,
  SafetyCertificateOutlined,
  SendOutlined,
  TeamOutlined,
  UnorderedListOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Button,
  Card,
  Col,
  ConfigProvider,
  Descriptions,
  Badge,
  Divider,
  Drawer,
  Empty,
  Form,
  Input,
  InputNumber,
  Layout,
  Modal,
  Progress,
  Radio,
  Row,
  Segmented,
  Select,
  Space,
  Spin,
  Statistic,
  Switch,
  Table,
  Tabs,
  Tag,
  Timeline,
  Typography,
  message,
  theme,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { Link, Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import {
  api,
  clearAuthSession,
  getAuthToken,
  getStoredUser,
  setAuthSession,
  type LoginInput,
  type ActivityInput,
  type ActivityType,
  type Agent,
  type AgentAction,
  type AgentActionInput,
  type AgentActionType,
  type AgentInput,
  type ApprovalRequest,
  type BulkCreateTaskInput,
  type Company,
  type CompanyInput,
  type Contact,
  type ContactDuplicateCandidate,
  type ContactInput,
  type ContactStatus,
  type Conversation,
  type ConversationStatus,
  type CrmTask,
  type Deal,
  type DealInput,
  type DealStatus,
  type EntityType,
  type MessageChannel,
  type MessageDirection,
  type MessageInput,
  type TaskInput,
  type TaskPriority,
  type TaskStatus,
  type WorkQueueBucket,
  type WorkQueueItem,
  type WorkQueueItemType,
} from './api/client';
import './App.css';

const { Content, Header, Sider } = Layout;
const { Text, Title } = Typography;
const jsonSchema = z.string().refine((value) => {
  try {
    JSON.parse(value);
    return true;
  } catch {
    return false;
  }
}, 'JSON is invalid');

const contactStatuses: ContactStatus[] = ['Lead', 'Active', 'Inactive', 'Archived'];
const dealStatuses: DealStatus[] = ['Open', 'Won', 'Lost', 'Canceled'];
const taskStatuses: TaskStatus[] = ['New', 'InProgress', 'Completed', 'Canceled'];
const taskPriorities: TaskPriority[] = ['Low', 'Normal', 'High', 'Urgent'];
const activityTypes: ActivityType[] = ['Note', 'Call', 'Email', 'TelegramMessage', 'Meeting', 'SystemEvent', 'AgentAction'];
const messageChannels: MessageChannel[] = ['Email', 'Telegram', 'WhatsApp', 'LinkedIn', 'WebsiteChat', 'Manual'];
const messageDirections: MessageDirection[] = ['Incoming', 'Outgoing'];
const conversationStatuses: ConversationStatus[] = ['Unread', 'WaitingOnUs', 'WaitingOnThem', 'Closed'];
const workQueueBuckets: WorkQueueBucket[] = ['Overdue', 'DueToday', 'ThisWeek', 'Upcoming', 'Unassigned'];
const workQueueTypes: WorkQueueItemType[] = ['Task', 'Activity'];
const actionTypes: AgentActionType[] = [
  'CreateContact',
  'UpdateContact',
  'CreateDeal',
  'UpdateDealStage',
  'CreateTask',
  'CompleteTask',
  'AddNote',
  'DraftMessage',
  'SendMessage',
  'RequestHumanApproval',
];
const entityTypes: EntityType[] = ['Contact', 'Company', 'Pipeline', 'PipelineStage', 'Deal', 'Task', 'Activity', 'Message', 'Agent', 'AgentAction', 'ApprovalRequest'];

type SelectOption = { label: string; value: string };

function formatDate(value?: string | null) {
  if (!value) return '—';
  return new Intl.DateTimeFormat('ru-RU', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
}

function money(value: number, currency = 'RUB') {
  return new Intl.NumberFormat('ru-RU', { style: 'currency', currency, maximumFractionDigits: 0 }).format(value);
}

function enumOptions<T extends string>(items: T[]) {
  return items.map((value) => ({ label: value, value }));
}

function compactId(value?: string | null) {
  return value ? value.slice(0, 8) : '—';
}

function toLocalDateTimeInput(value?: string | null) {
  if (!value) return undefined;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return undefined;
  const offset = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}

function toApiDateTime(value?: string | null) {
  return value ? new Date(value).toISOString() : null;
}

function linkedLabel(row: { contactName?: string | null; companyName?: string | null; dealTitle?: string | null }) {
  return [row.contactName, row.companyName, row.dealTitle].filter(Boolean).join(' · ') || 'General';
}

function statusColor(status: string) {
  return {
    Active: 'green',
    Lead: 'gold',
    Open: 'blue',
    Won: 'green',
    Lost: 'red',
    Canceled: 'default',
    Completed: 'green',
    InProgress: 'blue',
    New: 'gold',
    Proposed: 'gold',
    Approved: 'cyan',
    Executed: 'green',
    Failed: 'red',
    Pending: 'gold',
    Rejected: 'red',
    Urgent: 'red',
    High: 'volcano',
    Normal: 'blue',
    Low: 'default',
    Unread: 'red',
    WaitingOnUs: 'gold',
    WaitingOnThem: 'cyan',
    Closed: 'default',
    Overdue: 'red',
    DueToday: 'gold',
    ThisWeek: 'blue',
    Upcoming: 'default',
    Unassigned: 'purple',
  }[status] ?? 'default';
}

function StatusTag({ value }: { value?: string | null }) {
  if (!value) return <Text type="secondary">—</Text>;
  return <Tag color={statusColor(value)}>{value}</Tag>;
}

function JsonBlock({ value }: { value?: string | null }) {
  if (!value) return <Text type="secondary">—</Text>;
  let formatted: string;
  try {
    formatted = JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    formatted = value;
  }
  return <pre className="json-block">{formatted}</pre>;
}

function useNotifyMutation<TData, TVars>(mutationFn: (vars: TVars) => Promise<TData>, invalidate: string[]) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: async () => {
      await Promise.all(invalidate.map((key) => queryClient.invalidateQueries({ queryKey: [key] })));
      message.success('Saved');
    },
    onError: (error) => message.error(error instanceof Error ? error.message : 'Request failed'),
  });
}

function PageTitle({ title, extra }: { title: string; extra?: ReactNode }) {
  return (
    <div className="page-title">
      <Title level={2}>{title}</Title>
      <Space>{extra}</Space>
    </div>
  );
}

function SimpleList<T>({
  items,
  loading,
  emptyDescription,
  className,
  getKey,
  renderItem,
}: {
  items?: T[];
  loading?: boolean;
  emptyDescription?: ReactNode;
  className?: string;
  getKey?: (item: T, index: number) => Key;
  renderItem: (item: T) => ReactNode;
}) {
  const data = items ?? [];
  const listClassName = ['simple-list', className].filter(Boolean).join(' ');

  if (loading) {
    return (
      <div className={listClassName}>
        <div className="simple-list-empty"><Spin /></div>
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div className={listClassName}>
        <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={emptyDescription} />
      </div>
    );
  }

  return (
    <div className={listClassName}>
      {data.map((item, index) => (
        <div className="simple-list-item" key={getKey?.(item, index) ?? index}>
          {renderItem(item)}
        </div>
      ))}
    </div>
  );
}

function useLookups() {
  const companies = useQuery({ queryKey: ['companies'], queryFn: api.companies.list });
  const contacts = useQuery({ queryKey: ['contacts'], queryFn: api.contacts.list });
  const deals = useQuery({ queryKey: ['deals'], queryFn: api.deals.list });
  const agents = useQuery({ queryKey: ['agents'], queryFn: api.agents.list });
  const pipelines = useQuery({ queryKey: ['pipelines'], queryFn: api.pipelines.list });
  const pipelineId = pipelines.data?.find((x) => x.isDefault)?.id ?? pipelines.data?.[0]?.id;
  const stages = useQuery({
    queryKey: ['pipelineStages', pipelineId],
    queryFn: () => api.pipelines.stages(pipelineId!),
    enabled: Boolean(pipelineId),
  });

  return {
    companies,
    contacts,
    deals,
    agents,
    pipelines,
    stages,
    companyOptions: (companies.data ?? []).map((x) => ({ label: x.name, value: x.id })),
    contactOptions: (contacts.data ?? []).map((x) => ({ label: x.fullName || x.email || x.id, value: x.id })),
    dealOptions: (deals.data ?? []).map((x) => ({ label: x.title, value: x.id })),
    agentOptions: (agents.data ?? []).map((x) => ({ label: x.name, value: x.id })),
    pipelineOptions: (pipelines.data ?? []).map((x) => ({ label: x.name, value: x.id })),
    stageOptions: (stages.data ?? []).map((x) => ({ label: x.name, value: x.id })),
    defaultPipelineId: pipelineId,
  };
}

function LoginPage() {
  const navigate = useNavigate();
  const [form] = Form.useForm<LoginInput>();
  const [loading, setLoading] = useState(false);

  const onFinish = async (values: LoginInput) => {
    setLoading(true);
    try {
      const result = await api.auth.login(values);
      setAuthSession(result.token, result.user);
      navigate('/dashboard', { replace: true });
    } catch (error) {
      message.error(error instanceof Error ? error.message : 'Login failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <Card className="login-card">
        <Space orientation="vertical" size={4} style={{ width: '100%', textAlign: 'center', marginBottom: 16 }}>
          <span className="brand-mark">C</span>
          <Title level={3} style={{ margin: 0 }}>Crm Core</Title>
          <Text type="secondary">Sign in to continue</Text>
        </Space>
        <Form form={form} layout="vertical" onFinish={onFinish} requiredMark={false}>
          <Form.Item name="email" label="Email" rules={[{ required: true }, { type: 'email' }]}>
            <Input prefix={<UserOutlined />} placeholder="admin@crm.local" autoComplete="username" autoFocus />
          </Form.Item>
          <Form.Item name="password" label="Password" rules={[{ required: true }]}>
            <Input.Password prefix={<LockOutlined />} placeholder="Password" autoComplete="current-password" />
          </Form.Item>
          <Button type="primary" htmlType="submit" block loading={loading}>Sign in</Button>
        </Form>
      </Card>
    </div>
  );
}

function RequireAuth({ children }: { children: ReactNode }) {
  if (!getAuthToken()) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
}

function CurrentUserMenu() {
  const navigate = useNavigate();
  const me = useQuery({ queryKey: ['authMe'], queryFn: api.auth.me });
  const user = me.data ?? getStoredUser();

  const logout = () => {
    clearAuthSession();
    navigate('/login', { replace: true });
  };

  return (
    <Space>
      <UserOutlined />
      <Space orientation="vertical" size={0}>
        <Text strong>{user?.displayName ?? user?.email ?? '—'}</Text>
        <Text type="secondary" style={{ fontSize: 12 }}>{user?.role ?? ''}</Text>
      </Space>
      <Button icon={<LogoutOutlined />} onClick={logout}>Logout</Button>
    </Space>
  );
}

function Shell() {
  const navigate = useNavigate();
  const location = useLocation();
  const selectedKey = location.pathname.split('/')[1] || 'dashboard';

  const items = [
    { key: 'dashboard', icon: <DashboardOutlined />, label: 'Dashboard' },
    { key: 'contacts', icon: <TeamOutlined />, label: 'Contacts' },
    { key: 'companies', icon: <SafetyCertificateOutlined />, label: 'Companies' },
    { key: 'deals', icon: <UnorderedListOutlined />, label: 'Deals' },
    { key: 'activities', icon: <CheckOutlined />, label: 'Activities' },
    { key: 'timeline', icon: <UnorderedListOutlined />, label: 'Timeline' },
    { key: 'messages', icon: <MessageOutlined />, label: 'Messages' },
    { key: 'agents', icon: <RobotOutlined />, label: 'Agents' },
    { key: 'agent-actions', icon: <RobotOutlined />, label: 'Agent Actions' },
    { key: 'approvals', icon: <SafetyCertificateOutlined />, label: 'Approvals' },
  ];

  return (
    <Layout className="app-shell">
      <Sider width={252} breakpoint="lg" collapsedWidth={0} className="sidebar">
        <Link className="brand" to="/dashboard">
          <span className="brand-mark">C</span>
          <span>
            <strong>Crm Core</strong>
            <small>agent-ready MVP</small>
          </span>
        </Link>
        <MenuShim items={items} selectedKey={selectedKey} onSelect={(key) => navigate(`/${key}`)} />
      </Sider>
      <Layout>
        <Header className="topbar">
          <Space separator={<span className="topbar-dot" />}>
            <Text strong>API</Text>
            <Text type="secondary">{api.baseUrl}</Text>
          </Space>
          <Space size={16}>
            <Space>
              <Tag color="green">.NET 10</Tag>
              <Tag color="cyan">Hangfire</Tag>
              <Tag color="gold">PostgreSQL</Tag>
            </Space>
            <CurrentUserMenu />
          </Space>
        </Header>
        <Content className="workspace">
          <Routes>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/contacts" element={<ContactsPage />} />
            <Route path="/companies" element={<CompaniesPage />} />
            <Route path="/deals" element={<DealsPage />} />
            <Route path="/activities" element={<ActivitiesPage />} />
            <Route path="/tasks" element={<TasksPage />} />
            <Route path="/timeline" element={<TimelinePage />} />
            <Route path="/messages" element={<MessagesPage />} />
            <Route path="/agents" element={<AgentsPage />} />
            <Route path="/agent-actions" element={<AgentActionsPage />} />
            <Route path="/approvals" element={<ApprovalsPage />} />
          </Routes>
        </Content>
      </Layout>
    </Layout>
  );
}

function MenuShim({
  items,
  selectedKey,
  onSelect,
}: {
  items: { key: string; icon: ReactNode; label: string }[];
  selectedKey: string;
  onSelect: (key: string) => void;
}) {
  return (
    <nav className="nav-menu">
      {items.map((item) => (
        <button key={item.key} className={selectedKey === item.key ? 'active' : ''} onClick={() => onSelect(item.key)} type="button">
          {item.icon}
          <span>{item.label}</span>
        </button>
      ))}
    </nav>
  );
}

function DashboardPage() {
  const summary = useQuery({ queryKey: ['dashboard'], queryFn: api.dashboard });
  const deals = useQuery({ queryKey: ['deals'], queryFn: api.deals.list });
  const actions = useQuery({ queryKey: ['agentActions'], queryFn: api.agentActions.list });

  const openDeals = deals.data?.filter((x) => x.status === 'Open') ?? [];
  const total = openDeals.reduce((sum, deal) => sum + deal.amount, 0);

  return (
    <>
      <PageTitle title="Dashboard" />
      <Row gutter={[16, 16]}>
        <Col xs={24} md={8} xl={4}>
          <Card><Statistic title="Contacts" value={summary.data?.contacts ?? 0} loading={summary.isLoading} /></Card>
        </Col>
        <Col xs={24} md={8} xl={4}>
          <Card><Statistic title="Companies" value={summary.data?.companies ?? 0} loading={summary.isLoading} /></Card>
        </Col>
        <Col xs={24} md={8} xl={4}>
          <Card><Statistic title="Open deals" value={summary.data?.openDeals ?? 0} loading={summary.isLoading} /></Card>
        </Col>
        <Col xs={24} md={8} xl={4}>
          <Card><Statistic title="Pipeline" value={money(summary.data?.openDealAmount ?? total)} loading={summary.isLoading} /></Card>
        </Col>
        <Col xs={24} md={8} xl={4}>
          <Card><Statistic title="Open tasks" value={summary.data?.openTasks ?? 0} loading={summary.isLoading} /></Card>
        </Col>
        <Col xs={24} md={8} xl={4}>
          <Card><Statistic title="Approvals" value={summary.data?.pendingApprovals ?? 0} loading={summary.isLoading} /></Card>
        </Col>
      </Row>
      <Row gutter={[16, 16]} className="mt">
        <Col xs={24} xl={14}>
          <Card title="Open pipeline">
            <SimpleList
              items={openDeals.slice(0, 6)}
              getKey={(deal) => deal.id}
              renderItem={(deal) => (
                <>
                  <div className="simple-list-main">
                    <Text strong>{deal.title}</Text>
                    <Text type="secondary">{`${deal.companyName ?? 'No company'} · ${deal.stageName ?? 'No stage'}`}</Text>
                  </div>
                  <Space>
                    <Progress type="circle" size={42} percent={deal.probability} />
                    <Text strong>{money(deal.amount, deal.currency)}</Text>
                  </Space>
                </>
              )}
            />
          </Card>
        </Col>
        <Col xs={24} xl={10}>
          <Card title="Agent queue">
            <SimpleList
              items={(actions.data ?? []).slice(0, 6)}
              loading={actions.isLoading}
              getKey={(action) => action.id}
              renderItem={(action) => (
                <>
                  <div className="simple-list-main">
                    <Text strong>{action.actionType}</Text>
                    <Text type="secondary">{action.agentName ?? action.agentId}</Text>
                  </div>
                  <StatusTag value={action.status} />
                </>
              )}
            />
          </Card>
        </Col>
      </Row>
    </>
  );
}

function ContactsPage() {
  const [editing, setEditing] = useState<Contact | null>();
  const [viewing, setViewing] = useState<Contact | null>();
  const [duplicateDrawerOpen, setDuplicateDrawerOpen] = useState(false);
  const [selectedRowKeys, setSelectedRowKeys] = useState<Key[]>([]);
  const [bulkTaskOpen, setBulkTaskOpen] = useState(false);
  const query = useQuery({ queryKey: ['contacts'], queryFn: api.contacts.list });
  const duplicates = useQuery({ queryKey: ['contactDuplicates'], queryFn: api.contacts.duplicates });
  const lookups = useLookups();
  const remove = useNotifyMutation(api.contacts.delete, ['contacts', 'dashboard']);
  const merge = useNotifyMutation(api.contacts.merge, ['contacts', 'contactDuplicates', 'messages', 'activities', 'tasks', 'deals', 'dashboard']);

  const columns: ColumnsType<Contact> = [
    { title: 'Name', dataIndex: 'fullName', render: (_, row) => <Button type="link" onClick={() => setViewing(row)}>{row.fullName || row.email || row.id}</Button> },
    { title: 'Company', dataIndex: 'companyName' },
    { title: 'Email', dataIndex: 'email' },
    { title: 'Phone', dataIndex: 'phone' },
    { title: 'Status', dataIndex: 'status', render: StatusTag },
    { title: 'Updated', dataIndex: 'updatedAt', render: formatDate },
    {
      title: '',
      width: 132,
      render: (_, row) => (
        <Space>
          <Button icon={<EditOutlined />} onClick={() => setEditing(row)} />
          <Button danger icon={<DeleteOutlined />} onClick={() => remove.mutate(row.id)} />
        </Space>
      ),
    },
  ];

  return (
    <>
      <PageTitle
        title="Contacts"
        extra={(
          <>
            <Badge count={duplicates.data?.length ?? 0} size="small">
              <Button icon={<FilterOutlined />} onClick={() => setDuplicateDrawerOpen(true)}>Duplicates</Button>
            </Badge>
            <Button disabled={selectedRowKeys.length === 0} onClick={() => setBulkTaskOpen(true)}>Bulk task</Button>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing(null)}>New contact</Button>
          </>
        )}
      />
      <Table
        rowKey="id"
        loading={query.isLoading}
        dataSource={query.data}
        columns={columns}
        rowSelection={{
          selectedRowKeys,
          onChange: setSelectedRowKeys,
        }}
      />
      <ContactEditor open={editing !== undefined} contact={editing ?? undefined} companies={lookups.companyOptions} onClose={() => setEditing(undefined)} />
      <BulkTaskModal
        open={bulkTaskOpen}
        title="Create task for selected contacts"
        count={selectedRowKeys.length}
        onClose={() => setBulkTaskOpen(false)}
        onSubmit={(values) => api.contacts.bulkCreateTask({ ...values, contactIds: selectedRowKeys.map(String) })}
        invalidate={['contacts', 'tasks', 'activities', 'dashboard']}
      />
      <DuplicateContactsDrawer
        open={duplicateDrawerOpen}
        duplicates={duplicates.data ?? []}
        loading={duplicates.isLoading || merge.isPending}
        onClose={() => setDuplicateDrawerOpen(false)}
        onMerge={(candidate) => merge.mutate({
          primaryContactId: candidate.primaryContact.id,
          duplicateContactId: candidate.duplicateContact.id,
        })}
      />
      <Drawer open={Boolean(viewing)} title={viewing?.fullName || 'Contact'} onClose={() => setViewing(null)} size="large">
        {viewing && (
          <Descriptions column={1} bordered size="small">
            <Descriptions.Item label="Company">{viewing.companyName ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Email">{viewing.email ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Phone">{viewing.phone ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Telegram">{viewing.telegramUsername ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Position">{viewing.position ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Source">{viewing.source ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Status"><StatusTag value={viewing.status} /></Descriptions.Item>
          </Descriptions>
        )}
      </Drawer>
    </>
  );
}

function DuplicateContactsDrawer({
  open,
  duplicates,
  loading,
  onClose,
  onMerge,
}: {
  open: boolean;
  duplicates: ContactDuplicateCandidate[];
  loading: boolean;
  onClose: () => void;
  onMerge: (candidate: ContactDuplicateCandidate) => void;
}) {
  return (
    <Drawer open={open} title="Potential duplicates" onClose={onClose} size="large">
      <SimpleList
        loading={loading}
        items={duplicates}
        emptyDescription="No duplicates found"
        className="duplicate-list"
        getKey={(candidate) => `${candidate.primaryContact.id}-${candidate.duplicateContact.id}`}
        renderItem={(candidate) => (
          <>
            <div className="simple-list-main">
              <div>
                <Space wrap>
                  <StatusTag value={`${candidate.confidence}%`} />
                  <Text strong>{candidate.reason}</Text>
                </Space>
              </div>
              <Space orientation="vertical" size={4}>
                <Text>{candidate.primaryContact.fullName || candidate.primaryContact.email || candidate.primaryContact.id}</Text>
                <Text type="secondary">Duplicate: {candidate.duplicateContact.fullName || candidate.duplicateContact.email || candidate.duplicateContact.id}</Text>
                <Text type="secondary">{[candidate.primaryContact.email, candidate.primaryContact.phone, candidate.primaryContact.companyName].filter(Boolean).join(' · ')}</Text>
              </Space>
            </div>
            <Button type="primary" onClick={() => onMerge(candidate)}>Merge</Button>
          </>
        )}
      />
    </Drawer>
  );
}

function BulkTaskModal({
  open,
  title,
  count,
  onClose,
  onSubmit,
  invalidate,
}: {
  open: boolean;
  title: string;
  count: number;
  onClose: () => void;
  onSubmit: (values: BulkCreateTaskInput) => Promise<unknown>;
  invalidate: string[];
}) {
  const [form] = Form.useForm<BulkCreateTaskInput & { dueAtLocal?: string }>();
  const mutation = useNotifyMutation(onSubmit, invalidate);

  useEffect(() => {
    if (open) {
      form.setFieldsValue({ title: 'Follow up', priority: 'Normal' });
    }
  }, [form, open]);

  return (
    <Modal open={open} title={title} onCancel={onClose} onOk={() => form.submit()} confirmLoading={mutation.isPending} destroyOnHidden>
      <Form
        form={form}
        layout="vertical"
        onFinish={({ dueAtLocal, ...values }) => mutation.mutate({
          ...values,
          dueAt: toApiDateTime(dueAtLocal),
        }, { onSuccess: onClose })}
      >
        <Text type="secondary">Selected records: {count}</Text>
        <Form.Item name="title" label="Task title" rules={[{ required: true }]}><Input /></Form.Item>
        <Form.Item name="description" label="Description"><Input.TextArea rows={3} /></Form.Item>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="priority" label="Priority"><Select options={enumOptions(taskPriorities)} /></Form.Item></Col>
          <Col span={12}><Form.Item name="dueAtLocal" label="Due"><Input type="datetime-local" /></Form.Item></Col>
        </Row>
      </Form>
    </Modal>
  );
}

function ContactEditor({ open, contact, companies, onClose }: { open: boolean; contact?: Contact; companies: SelectOption[]; onClose: () => void }) {
  const [form] = Form.useForm<ContactInput>();
  const mutation = useNotifyMutation((values: ContactInput) => contact ? api.contacts.update(contact.id, values) : api.contacts.create(values), ['contacts', 'dashboard']);

  useEffect(() => {
    if (open) form.setFieldsValue(contact ?? { status: 'Lead' });
  }, [contact, form, open]);

  return (
    <Modal open={open} title={contact ? 'Edit contact' : 'New contact'} onCancel={onClose} onOk={() => form.submit()} confirmLoading={mutation.isPending} destroyOnHidden>
      <Form form={form} layout="vertical" onFinish={(values) => mutation.mutate(values, { onSuccess: onClose })}>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="firstName" label="First name"><Input /></Form.Item></Col>
          <Col span={12}><Form.Item name="lastName" label="Last name"><Input /></Form.Item></Col>
        </Row>
        <Form.Item name="companyId" label="Company"><Select allowClear options={companies} showSearch optionFilterProp="label" /></Form.Item>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="email" label="Email"><Input /></Form.Item></Col>
          <Col span={12}><Form.Item name="phone" label="Phone"><Input /></Form.Item></Col>
        </Row>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="telegramUsername" label="Telegram"><Input /></Form.Item></Col>
          <Col span={12}><Form.Item name="status" label="Status"><Select options={enumOptions(contactStatuses)} /></Form.Item></Col>
        </Row>
        <Form.Item name="position" label="Position"><Input /></Form.Item>
        <Form.Item name="source" label="Source"><Input /></Form.Item>
      </Form>
    </Modal>
  );
}

function CompaniesPage() {
  const [editing, setEditing] = useState<Company | null>();
  const [viewing, setViewing] = useState<Company | null>();
  const query = useQuery({ queryKey: ['companies'], queryFn: api.companies.list });
  const remove = useNotifyMutation(api.companies.delete, ['companies', 'contacts', 'dashboard']);

  const columns: ColumnsType<Company> = [
    { title: 'Name', dataIndex: 'name', render: (_, row) => <Button type="link" onClick={() => setViewing(row)}>{row.name}</Button> },
    { title: 'Website', dataIndex: 'website', render: (value) => value ? <a href={value} target="_blank">{value}</a> : '—' },
    { title: 'Email', dataIndex: 'email' },
    { title: 'Phone', dataIndex: 'phone' },
    { title: 'Updated', dataIndex: 'updatedAt', render: formatDate },
    {
      title: '',
      width: 132,
      render: (_, row) => (
        <Space>
          <Button icon={<EditOutlined />} onClick={() => setEditing(row)} />
          <Button danger icon={<DeleteOutlined />} onClick={() => remove.mutate(row.id)} />
        </Space>
      ),
    },
  ];

  return (
    <>
      <PageTitle title="Companies" extra={<Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing(null)}>New company</Button>} />
      <Table rowKey="id" loading={query.isLoading} dataSource={query.data} columns={columns} />
      <CompanyEditor open={editing !== undefined} company={editing ?? undefined} onClose={() => setEditing(undefined)} />
      <Drawer open={Boolean(viewing)} title={viewing?.name} onClose={() => setViewing(null)} size="large">
        {viewing && (
          <Descriptions column={1} bordered size="small">
            <Descriptions.Item label="Legal name">{viewing.legalName ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="INN">{viewing.inn ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Website">{viewing.website ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Email">{viewing.email ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Phone">{viewing.phone ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Address">{viewing.address ?? '—'}</Descriptions.Item>
          </Descriptions>
        )}
      </Drawer>
    </>
  );
}

function CompanyEditor({ open, company, onClose }: { open: boolean; company?: Company; onClose: () => void }) {
  const [form] = Form.useForm<CompanyInput>();
  const mutation = useNotifyMutation((values: CompanyInput) => company ? api.companies.update(company.id, values) : api.companies.create(values), ['companies', 'dashboard']);

  useEffect(() => {
    if (open) form.setFieldsValue(company ?? {});
  }, [company, form, open]);

  return (
    <Modal open={open} title={company ? 'Edit company' : 'New company'} onCancel={onClose} onOk={() => form.submit()} confirmLoading={mutation.isPending} destroyOnHidden>
      <Form form={form} layout="vertical" onFinish={(values) => mutation.mutate(values, { onSuccess: onClose })}>
        <Form.Item name="name" label="Name" rules={[{ required: true }]}><Input /></Form.Item>
        <Form.Item name="legalName" label="Legal name"><Input /></Form.Item>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="inn" label="INN"><Input /></Form.Item></Col>
          <Col span={12}><Form.Item name="website" label="Website"><Input /></Form.Item></Col>
        </Row>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="email" label="Email"><Input /></Form.Item></Col>
          <Col span={12}><Form.Item name="phone" label="Phone"><Input /></Form.Item></Col>
        </Row>
        <Form.Item name="address" label="Address"><Input.TextArea rows={3} /></Form.Item>
      </Form>
    </Modal>
  );
}

function DealsPage() {
  const [editing, setEditing] = useState<Deal | null>();
  const [selectedRowKeys, setSelectedRowKeys] = useState<Key[]>([]);
  const [bulkTaskOpen, setBulkTaskOpen] = useState(false);
  const query = useQuery({ queryKey: ['deals'], queryFn: api.deals.list });
  const lookups = useLookups();
  const move = useNotifyMutation(({ id, stageId }: { id: string; stageId: string }) => api.deals.moveStage(id, stageId), ['deals', 'dashboard', 'activities']);
  const remove = useNotifyMutation(api.deals.delete, ['deals', 'dashboard']);

  const columns: ColumnsType<Deal> = [
    { title: 'Deal', dataIndex: 'title' },
    { title: 'Company', dataIndex: 'companyName' },
    { title: 'Stage', dataIndex: 'stageName' },
    { title: 'Amount', render: (_, row) => money(row.amount, row.currency) },
    { title: 'Probability', dataIndex: 'probability', render: (value) => <Progress percent={value} size="small" /> },
    { title: 'Status', dataIndex: 'status', render: StatusTag },
    {
      title: '',
      width: 132,
      render: (_, row) => (
        <Space>
          <Button icon={<EditOutlined />} onClick={() => setEditing(row)} />
          <Button danger icon={<DeleteOutlined />} onClick={() => remove.mutate(row.id)} />
        </Space>
      ),
    },
  ];

  return (
    <>
      <PageTitle
        title="Deals"
        extra={(
          <>
            <Button disabled={selectedRowKeys.length === 0} onClick={() => setBulkTaskOpen(true)}>Bulk task</Button>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing(null)}>New deal</Button>
          </>
        )}
      />
      <Tabs
        items={[
          {
            key: 'table',
            label: 'Table',
            children: (
              <Table
                rowKey="id"
                loading={query.isLoading}
                dataSource={query.data}
                columns={columns}
                rowSelection={{ selectedRowKeys, onChange: setSelectedRowKeys }}
              />
            ),
          },
          {
            key: 'kanban',
            label: 'Kanban',
            children: (
              <div className="kanban">
                {(lookups.stages.data ?? []).map((stage) => (
                  <section key={stage.id} className="kanban-column">
                    <div className="kanban-title">
                      <Text strong>{stage.name}</Text>
                      <Tag>{query.data?.filter((deal) => deal.stageId === stage.id).length ?? 0}</Tag>
                    </div>
                    {(query.data ?? []).filter((deal) => deal.stageId === stage.id).map((deal) => (
                      <Card key={deal.id} className="deal-card" size="small">
                        <Space orientation="vertical" size={8} style={{ width: '100%' }}>
                          <Text strong>{deal.title}</Text>
                          <Text type="secondary">{deal.companyName ?? deal.contactName ?? 'No account'}</Text>
                          <Space className="spread">
                            <Text>{money(deal.amount, deal.currency)}</Text>
                            <StatusTag value={deal.status} />
                          </Space>
                          <Select value={deal.stageId} options={lookups.stageOptions} onChange={(stageId) => move.mutate({ id: deal.id, stageId })} />
                        </Space>
                      </Card>
                    ))}
                  </section>
                ))}
              </div>
            ),
          },
        ]}
      />
      <BulkTaskModal
        open={bulkTaskOpen}
        title="Create task for selected deals"
        count={selectedRowKeys.length}
        onClose={() => setBulkTaskOpen(false)}
        onSubmit={(values) => api.deals.bulkCreateTask({ ...values, dealIds: selectedRowKeys.map(String) })}
        invalidate={['deals', 'tasks', 'activities', 'dashboard']}
      />
      <DealEditor open={editing !== undefined} deal={editing ?? undefined} lookups={lookups} onClose={() => setEditing(undefined)} />
    </>
  );
}

function DealEditor({ open, deal, lookups, onClose }: { open: boolean; deal?: Deal; lookups: ReturnType<typeof useLookups>; onClose: () => void }) {
  const [form] = Form.useForm<DealInput>();
  const mutation = useNotifyMutation((values: DealInput) => deal ? api.deals.update(deal.id, values) : api.deals.create(values), ['deals', 'dashboard']);

  useEffect(() => {
    if (open) {
      form.setFieldsValue(deal ?? { currency: 'RUB', status: 'Open', pipelineId: lookups.defaultPipelineId, stageId: lookups.stageOptions[0]?.value });
    }
  }, [deal, form, lookups.defaultPipelineId, lookups.stageOptions, open]);

  return (
    <Modal open={open} title={deal ? 'Edit deal' : 'New deal'} onCancel={onClose} onOk={() => form.submit()} confirmLoading={mutation.isPending} destroyOnHidden>
      <Form form={form} layout="vertical" onFinish={(values) => mutation.mutate(values, { onSuccess: onClose })}>
        <Form.Item name="title" label="Title" rules={[{ required: true }]}><Input /></Form.Item>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="companyId" label="Company"><Select allowClear options={lookups.companyOptions} showSearch optionFilterProp="label" /></Form.Item></Col>
          <Col span={12}><Form.Item name="contactId" label="Contact"><Select allowClear options={lookups.contactOptions} showSearch optionFilterProp="label" /></Form.Item></Col>
        </Row>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="pipelineId" label="Pipeline" rules={[{ required: true }]}><Select options={lookups.pipelineOptions} /></Form.Item></Col>
          <Col span={12}><Form.Item name="stageId" label="Stage" rules={[{ required: true }]}><Select options={lookups.stageOptions} /></Form.Item></Col>
        </Row>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="amount" label="Amount" rules={[{ required: true }]}><InputNumber min={0} style={{ width: '100%' }} /></Form.Item></Col>
          <Col span={12}><Form.Item name="currency" label="Currency" rules={[{ required: true }]}><Input maxLength={3} /></Form.Item></Col>
        </Row>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="probability" label="Probability"><InputNumber min={0} max={100} style={{ width: '100%' }} /></Form.Item></Col>
          <Col span={12}><Form.Item name="status" label="Status"><Select options={enumOptions(dealStatuses)} /></Form.Item></Col>
        </Row>
        <Form.Item name="source" label="Source"><Input /></Form.Item>
      </Form>
    </Modal>
  );
}

function ActivitiesPage() {
  const [taskEditorOpen, setTaskEditorOpen] = useState(false);
  const [activityEditorOpen, setActivityEditorOpen] = useState(false);
  const [bucket, setBucket] = useState<WorkQueueBucket | 'All'>('All');
  const [type, setType] = useState<WorkQueueItemType | 'All'>('All');
  const [view, setView] = useState<'table' | 'grouped'>('table');
  const query = useQuery({ queryKey: ['workQueue'], queryFn: api.activities.workQueue });
  const lookups = useLookups();
  const complete = useNotifyMutation(api.tasks.complete, ['workQueue', 'tasks', 'dashboard']);
  const cancel = useNotifyMutation(api.tasks.cancel, ['workQueue', 'tasks', 'dashboard']);

  const items = query.data ?? [];
  const visibleItems = items.filter((item) =>
    (bucket === 'All' || item.bucket === bucket) &&
    (type === 'All' || item.type === type));
  const bucketCount = (value: WorkQueueBucket) => items.filter((item) => item.bucket === value).length;
  const unassignedCount = items.filter((item) => item.responsibleUserId == null && item.type === 'Task').length;

  const columns: ColumnsType<WorkQueueItem> = [
    {
      title: 'Type',
      dataIndex: 'type',
      width: 118,
      render: (_, row) => <Tag color={row.type === 'Task' ? 'blue' : 'green'}>{row.activityType ?? row.type}</Tag>,
    },
    {
      title: 'Title',
      dataIndex: 'title',
      render: (_, row) => (
        <Space orientation="vertical" size={2}>
          <Text strong>{row.title}</Text>
          {row.description && <Text type="secondary" ellipsis>{row.description}</Text>}
        </Space>
      ),
    },
    { title: 'Bucket', dataIndex: 'bucket', width: 130, render: StatusTag },
    { title: 'Priority', dataIndex: 'priority', width: 120, render: StatusTag },
    { title: 'Linked', width: 260, render: (_, row) => <Text type="secondary">{linkedLabel(row)}</Text> },
    { title: 'Assignee', dataIndex: 'responsibleUserId', width: 120, render: compactId },
    { title: 'Due / start', width: 160, render: (_, row) => formatDate(row.dueAt ?? row.startedAt) },
    {
      title: '',
      width: 132,
      render: (_, row) => row.type === 'Task' ? (
        <Space>
          <Button icon={<CheckOutlined />} onClick={() => complete.mutate(row.sourceId)} />
          <Button danger icon={<CloseOutlined />} onClick={() => cancel.mutate(row.sourceId)} />
        </Space>
      ) : null,
    },
  ];

  return (
    <>
      <PageTitle
        title="Activities"
        extra={(
          <>
            <Button onClick={() => setActivityEditorOpen(true)}>Add activity</Button>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setTaskEditorOpen(true)}>New task</Button>
          </>
        )}
      />
      <div className="queue-toolbar">
        <Space wrap>
          <Button type={bucket === 'All' ? 'primary' : 'default'} onClick={() => setBucket('All')}>All {items.length}</Button>
          <Button danger={bucketCount('Overdue') > 0} type={bucket === 'Overdue' ? 'primary' : 'default'} onClick={() => setBucket('Overdue')}>Overdue {bucketCount('Overdue')}</Button>
          <Button type={bucket === 'DueToday' ? 'primary' : 'default'} onClick={() => setBucket('DueToday')}>Today {bucketCount('DueToday')}</Button>
          <Button type={bucket === 'ThisWeek' ? 'primary' : 'default'} onClick={() => setBucket('ThisWeek')}>This week {bucketCount('ThisWeek')}</Button>
          <Button type={bucket === 'Unassigned' ? 'primary' : 'default'} onClick={() => setBucket('Unassigned')}>Unassigned {unassignedCount}</Button>
        </Space>
        <Space wrap>
          <Select value={type} onChange={setType} options={[{ label: 'All types', value: 'All' }, ...enumOptions(workQueueTypes)]} style={{ width: 150 }} />
          <Segmented value={view} onChange={(value) => setView(value as 'table' | 'grouped')} options={[{ label: 'Table', value: 'table' }, { label: 'Grouped', value: 'grouped' }]} />
        </Space>
      </div>
      {view === 'table' ? (
        <Table rowKey="id" loading={query.isLoading} dataSource={visibleItems} columns={columns} />
      ) : (
        <div className="queue-groups">
          {workQueueBuckets.map((group) => {
            const groupItems = visibleItems.filter((item) => item.bucket === group);
            if (groupItems.length === 0) return null;

            return (
              <section className="queue-group" key={group}>
                <div className="queue-group-title">
                  <StatusTag value={group} />
                  <Tag>{groupItems.length}</Tag>
                </div>
                <SimpleList
                  items={groupItems}
                  className="queue-list"
                  getKey={(item) => item.id}
                  renderItem={(item) => (
                    <>
                      <div className="simple-list-main">
                        <Space wrap>
                          <Tag>{item.activityType ?? item.type}</Tag>
                          <Text strong>{item.title}</Text>
                        </Space>
                        <Text type="secondary">{`${linkedLabel(item)} · ${formatDate(item.dueAt ?? item.startedAt)}`}</Text>
                      </div>
                      {item.type === 'Task' ? (
                        <Space>
                          <Button size="small" icon={<CheckOutlined />} onClick={() => complete.mutate(item.sourceId)} />
                          <Button size="small" danger icon={<CloseOutlined />} onClick={() => cancel.mutate(item.sourceId)} />
                        </Space>
                      ) : null}
                    </>
                  )}
                />
              </section>
            );
          })}
        </div>
      )}
      <TaskEditor open={taskEditorOpen} lookups={lookups} onClose={() => setTaskEditorOpen(false)} />
      <ActivityEditor open={activityEditorOpen} lookups={lookups} onClose={() => setActivityEditorOpen(false)} />
    </>
  );
}

function TasksPage() {
  const [editing, setEditing] = useState<CrmTask | null>();
  const [status, setStatus] = useState<TaskStatus | undefined>();
  const query = useQuery({ queryKey: ['tasks', status], queryFn: () => api.tasks.list(status) });
  const lookups = useLookups();
  const complete = useNotifyMutation(api.tasks.complete, ['tasks', 'dashboard']);
  const remove = useNotifyMutation(api.tasks.delete, ['tasks', 'dashboard']);

  const columns: ColumnsType<CrmTask> = [
    { title: 'Task', dataIndex: 'title' },
    { title: 'Deal', dataIndex: 'dealTitle' },
    { title: 'Due', dataIndex: 'dueAt', render: formatDate },
    { title: 'Priority', dataIndex: 'priority', render: StatusTag },
    { title: 'Status', dataIndex: 'status', render: StatusTag },
    {
      title: '',
      width: 180,
      render: (_, row) => (
        <Space>
          <Button icon={<CheckOutlined />} disabled={row.status === 'Completed'} onClick={() => complete.mutate(row.id)} />
          <Button icon={<EditOutlined />} onClick={() => setEditing(row)} />
          <Button danger icon={<DeleteOutlined />} onClick={() => remove.mutate(row.id)} />
        </Space>
      ),
    },
  ];

  return (
    <>
      <PageTitle title="Tasks" extra={<><Select allowClear placeholder="Status" value={status} onChange={setStatus} options={enumOptions(taskStatuses)} style={{ width: 180 }} /><Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing(null)}>New task</Button></>} />
      <Table rowKey="id" loading={query.isLoading} dataSource={query.data} columns={columns} />
      <TaskEditor open={editing !== undefined} task={editing ?? undefined} lookups={lookups} onClose={() => setEditing(undefined)} />
    </>
  );
}

function TaskEditor({ open, task, lookups, onClose }: { open: boolean; task?: CrmTask; lookups: ReturnType<typeof useLookups>; onClose: () => void }) {
  const [form] = Form.useForm<TaskInput & { dueAtLocal?: string }>();
  const mutation = useNotifyMutation((values: TaskInput) => task ? api.tasks.update(task.id, values) : api.tasks.create(values), ['tasks', 'dashboard']);

  useEffect(() => {
    if (open) form.setFieldsValue(task ? { ...task, dueAtLocal: toLocalDateTimeInput(task.dueAt) } : { status: 'New', priority: 'Normal' });
  }, [form, open, task]);

  return (
    <Modal open={open} title={task ? 'Edit task' : 'New task'} onCancel={onClose} onOk={() => form.submit()} confirmLoading={mutation.isPending} destroyOnHidden>
      <Form
        form={form}
        layout="vertical"
        onFinish={({ dueAtLocal, ...values }) => mutation.mutate({
          ...values,
          dueAt: toApiDateTime(dueAtLocal),
        }, { onSuccess: onClose })}
      >
        <Form.Item name="title" label="Title" rules={[{ required: true }]}><Input /></Form.Item>
        <Form.Item name="description" label="Description"><Input.TextArea rows={3} /></Form.Item>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="status" label="Status"><Select options={enumOptions(taskStatuses)} /></Form.Item></Col>
          <Col span={12}><Form.Item name="priority" label="Priority"><Select options={enumOptions(taskPriorities)} /></Form.Item></Col>
        </Row>
        <Form.Item name="dueAtLocal" label="Due"><Input type="datetime-local" /></Form.Item>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="contactId" label="Contact"><Select allowClear options={lookups.contactOptions} /></Form.Item></Col>
          <Col span={12}><Form.Item name="companyId" label="Company"><Select allowClear options={lookups.companyOptions} /></Form.Item></Col>
        </Row>
        <Form.Item name="dealId" label="Deal"><Select allowClear options={lookups.dealOptions} /></Form.Item>
      </Form>
    </Modal>
  );
}

function TimelinePage() {
  const [open, setOpen] = useState(false);
  const query = useQuery({ queryKey: ['activities'], queryFn: api.activities.list });
  const lookups = useLookups();

  return (
    <>
      <PageTitle title="Timeline" extra={<Button type="primary" icon={<PlusOutlined />} onClick={() => setOpen(true)}>Add activity</Button>} />
      <Card>
        <Timeline
          items={(query.data ?? []).map((item) => ({
            color: item.type === 'SystemEvent' ? 'blue' : item.type === 'AgentAction' ? 'green' : 'gray',
            children: (
              <Space orientation="vertical" size={2}>
                <Space><Tag>{item.type}</Tag><Text strong>{item.title}</Text></Space>
                <Text type="secondary">{[item.contactName, item.companyName, item.dealTitle].filter(Boolean).join(' · ') || 'General'}</Text>
                {item.description && <Text>{item.description}</Text>}
                <Text type="secondary">{formatDate(item.createdAt)}</Text>
              </Space>
            ),
          }))}
        />
      </Card>
      <ActivityEditor open={open} lookups={lookups} onClose={() => setOpen(false)} />
    </>
  );
}

function ActivityEditor({ open, lookups, onClose }: { open: boolean; lookups: ReturnType<typeof useLookups>; onClose: () => void }) {
  const [form] = Form.useForm<ActivityInput>();
  const mutation = useNotifyMutation(api.activities.create, ['activities', 'dashboard']);

  useEffect(() => {
    if (open) form.setFieldsValue({ type: 'Note' });
  }, [form, open]);

  return (
    <Modal open={open} title="Add activity" onCancel={onClose} onOk={() => form.submit()} confirmLoading={mutation.isPending} destroyOnHidden>
      <Form form={form} layout="vertical" onFinish={(values) => mutation.mutate(values, { onSuccess: onClose })}>
        <Form.Item name="type" label="Type"><Select options={enumOptions(activityTypes)} /></Form.Item>
        <Form.Item name="title" label="Title" rules={[{ required: true }]}><Input /></Form.Item>
        <Form.Item name="description" label="Description"><Input.TextArea rows={4} /></Form.Item>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="contactId" label="Contact"><Select allowClear options={lookups.contactOptions} /></Form.Item></Col>
          <Col span={12}><Form.Item name="companyId" label="Company"><Select allowClear options={lookups.companyOptions} /></Form.Item></Col>
        </Row>
        <Form.Item name="dealId" label="Deal"><Select allowClear options={lookups.dealOptions} /></Form.Item>
      </Form>
    </Modal>
  );
}

function MessagesPage() {
  const [open, setOpen] = useState(false);
  const [status, setStatus] = useState<ConversationStatus | 'All'>('All');
  const [selectedId, setSelectedId] = useState<string>();
  const [aiOpen, setAiOpen] = useState(true);
  const [form] = Form.useForm<MessageInput>();
  const query = useQuery({ queryKey: ['conversations'], queryFn: api.messages.conversations });
  const lookups = useLookups();
  const create = useNotifyMutation(api.messages.create, ['conversations', 'messages', 'dashboard']);

  const conversations = query.data ?? [];
  const filtered = conversations.filter((item) => status === 'All' || item.status === status);
  // selectedId is only set on explicit user clicks; the fallback keeps the first
  // conversation visually selected without syncing state in an effect.
  const selected = conversations.find((item) => item.id === selectedId) ?? filtered[0] ?? conversations[0];

  useEffect(() => {
    if (selected) {
      form.setFieldsValue({
        channel: selected.lastChannel,
        direction: 'Outgoing',
        contactId: selected.contactId,
        dealId: selected.dealId,
      });
    }
  }, [form, selected]);

  return (
    <>
      <PageTitle
        title="Messages"
        extra={(
          <>
            <Button icon={<RobotOutlined />} onClick={() => setAiOpen((value) => !value)}>{aiOpen ? 'Hide AI' : 'Show AI'}</Button>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setOpen(true)}>New message</Button>
          </>
        )}
      />
      <div className={`conversation-workspace ${aiOpen ? '' : 'ai-collapsed'}`}>
        <aside className="conversation-list">
          <Input.Search placeholder="Search contact, company, message" allowClear className="conversation-search" />
          <Radio.Group value={status} onChange={(event) => setStatus(event.target.value)} className="conversation-filters">
            <Radio.Button value="All">All {conversations.length}</Radio.Button>
            {conversationStatuses.map((value) => (
              <Radio.Button key={value} value={value}>{value} {conversations.filter((item) => item.status === value).length}</Radio.Button>
            ))}
          </Radio.Group>
          <div className="conversation-list-scroll">
            {filtered.map((conversation) => (
              <button
                type="button"
                key={conversation.id}
                className={`conversation-item ${selected?.id === conversation.id ? 'active' : ''}`}
                onClick={() => setSelectedId(conversation.id)}
              >
                <span className="avatar-token">{(conversation.contactName ?? conversation.companyName ?? '?').slice(0, 2).toUpperCase()}</span>
                <span className="conversation-item-main">
                  <span className="conversation-row">
                    <Text strong>{conversation.contactName ?? conversation.companyName ?? conversation.dealTitle ?? 'Unknown'}</Text>
                    <Text type="secondary">{formatDate(conversation.lastMessageAt)}</Text>
                  </span>
                  <Text type="secondary" ellipsis>{conversation.lastMessageText}</Text>
                  <span className="conversation-row">
                    <Space size={4}><StatusTag value={conversation.lastChannel} /><StatusTag value={conversation.status} /></Space>
                    {conversation.openTaskCount > 0 && <Tag color="gold">{conversation.openTaskCount} tasks</Tag>}
                  </span>
                </span>
              </button>
            ))}
          </div>
        </aside>
        <section className="conversation-thread">
          {selected ? (
            <>
              <div className="thread-header">
                <Space orientation="vertical" size={2}>
                  <Title level={4}>{selected.contactName ?? selected.companyName ?? selected.dealTitle ?? 'Conversation'}</Title>
                  <Text type="secondary">{linkedLabel(selected)} · {selected.messageCount} messages</Text>
                </Space>
                <Space><StatusTag value={selected.status} /><StatusTag value={selected.lastChannel} /></Space>
              </div>
              <div className="message-timeline">
                {selected.messages.map((item) => (
                  <div key={item.id} className={`message-row ${item.direction === 'Outgoing' ? 'outgoing' : 'incoming'}`}>
                    <div className="message-meta">
                      <Text type="secondary">{item.contactName ?? item.direction}</Text>
                      <Text type="secondary">{item.channel} · {formatDate(item.receivedAt ?? item.sentAt ?? item.createdAt)}</Text>
                    </div>
                    <div className="message-bubble">{item.text}</div>
                  </div>
                ))}
              </div>
              <Form
                form={form}
                className="message-composer"
                layout="vertical"
                onFinish={(values) => create.mutate(values, { onSuccess: () => form.setFieldValue('text', '') })}
              >
                <Row gutter={8}>
                  <Col xs={24} md={8}><Form.Item name="channel" noStyle><Select options={enumOptions(messageChannels)} /></Form.Item></Col>
                  <Col xs={24} md={8}><Form.Item name="direction" noStyle><Select options={enumOptions(messageDirections)} /></Form.Item></Col>
                  <Col xs={24} md={8}><Button icon={<SendOutlined />} type="primary" htmlType="submit" block>Send</Button></Col>
                </Row>
                <Form.Item name="contactId" hidden><Input /></Form.Item>
                <Form.Item name="dealId" hidden><Input /></Form.Item>
                <Form.Item name="text" rules={[{ required: true }]}><Input.TextArea rows={3} placeholder="Write a message..." /></Form.Item>
              </Form>
            </>
          ) : (
            <Empty description="No conversations yet" />
          )}
        </section>
        {aiOpen && <ContextualAiPanel conversation={selected} />}
      </div>
      <MessageEditor open={open} lookups={lookups} onClose={() => setOpen(false)} />
    </>
  );
}

function ContextualAiPanel({ conversation }: { conversation?: Conversation }) {
  const [prompt, setPrompt] = useState('');
  const lookups = useLookups();
  const actions = useQuery({ queryKey: ['agentActions'], queryFn: api.agentActions.list });
  const createAction = useNotifyMutation(api.agentActions.create, ['agentActions', 'approvals', 'dashboard']);
  const approve = useNotifyMutation(api.agentActions.approve, ['agentActions', 'approvals', 'dashboard']);
  const reject = useNotifyMutation(api.agentActions.reject, ['agentActions', 'approvals', 'dashboard']);
  const execute = useNotifyMutation(api.agentActions.execute, ['agentActions', 'messages', 'activities', 'tasks', 'dashboard']);
  const defaultAgent = (lookups.agents.data ?? []).find((agent) => agent.isActive) ?? lookups.agents.data?.[0];
  const relatedActions = (actions.data ?? [])
    .filter((action) =>
      conversation &&
      (action.targetEntityId === conversation.contactId || action.targetEntityId === conversation.dealId || action.targetEntityId == null) &&
      ['DraftMessage', 'SendMessage', 'AddNote', 'CreateTask'].includes(action.actionType))
    .slice(0, 5);

  const proposeDraft = () => {
    if (!conversation || !defaultAgent) {
      message.warning('Create an active agent first.');
      return;
    }

    const text = prompt.trim() || `Prepare a concise reply for ${conversation.contactName ?? conversation.companyName ?? 'this conversation'}.`;
    createAction.mutate({
      agentId: defaultAgent.id,
      actionType: 'DraftMessage',
      targetEntityType: conversation.contactId ? 'Contact' : conversation.dealId ? 'Deal' : undefined,
      targetEntityId: conversation.contactId ?? conversation.dealId ?? undefined,
      inputJson: JSON.stringify({
        channel: conversation.lastChannel,
        direction: 'Outgoing',
        contactId: conversation.contactId,
        dealId: conversation.dealId,
        text,
      }),
      reasoningSummary: `Draft reply for ${conversation.contactName ?? conversation.companyName ?? conversation.dealTitle ?? 'conversation'}.`,
      requiresApproval: true,
    }, { onSuccess: () => setPrompt('') });
  };

  return (
    <aside className="ai-panel">
      <div className="ai-panel-header">
        <Space>
          <RobotOutlined />
          <Text strong>AI assistant</Text>
        </Space>
        <Tag color="gold">Approval first</Tag>
      </div>
      <Text type="secondary">
        {conversation ? `Context: ${conversation.contactName ?? conversation.companyName ?? conversation.dealTitle ?? conversation.id}` : 'Select a conversation to give the agent context.'}
      </Text>
      <Input.TextArea
        rows={4}
        value={prompt}
        onChange={(event) => setPrompt(event.target.value)}
        placeholder="Ask the agent to draft a reply or summarize the next step..."
      />
      <Button type="primary" icon={<RobotOutlined />} disabled={!conversation} onClick={proposeDraft}>Propose draft</Button>
      <Divider />
      <Text strong>Recent proposed actions</Text>
      <SimpleList
        items={relatedActions}
        className="ai-action-list"
        getKey={(action) => action.id}
        renderItem={(action) => (
          <Space orientation="vertical" size={6} style={{ width: '100%' }}>
            <Space className="spread">
              <Text>{action.actionType}</Text>
              <StatusTag value={action.status} />
            </Space>
            <Text type="secondary" ellipsis>{action.reasoningSummary ?? action.inputJson}</Text>
            <Space>
              <Button size="small" icon={<CheckOutlined />} disabled={!['Proposed', 'Failed'].includes(action.status)} onClick={() => approve.mutate(action.id)} />
              <Button size="small" danger icon={<CloseOutlined />} disabled={!['Proposed', 'Approved'].includes(action.status)} onClick={() => reject.mutate(action.id)} />
              <Button size="small" type="primary" disabled={action.requiresApproval && !['Approved', 'Failed'].includes(action.status)} onClick={() => execute.mutate(action.id)}>Run</Button>
            </Space>
          </Space>
        )}
      />
    </aside>
  );
}

function MessageEditor({ open, lookups, onClose }: { open: boolean; lookups: ReturnType<typeof useLookups>; onClose: () => void }) {
  const [form] = Form.useForm<MessageInput>();
  const mutation = useNotifyMutation(api.messages.create, ['messages']);

  useEffect(() => {
    if (open) form.setFieldsValue({ channel: 'Manual', direction: 'Outgoing' });
  }, [form, open]);

  return (
    <Modal open={open} title="New message" onCancel={onClose} onOk={() => form.submit()} confirmLoading={mutation.isPending} destroyOnHidden>
      <Form form={form} layout="vertical" onFinish={(values) => mutation.mutate(values, { onSuccess: onClose })}>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="channel" label="Channel"><Select options={enumOptions(messageChannels)} /></Form.Item></Col>
          <Col span={12}><Form.Item name="direction" label="Direction"><Select options={enumOptions(messageDirections)} /></Form.Item></Col>
        </Row>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="contactId" label="Contact"><Select allowClear options={lookups.contactOptions} /></Form.Item></Col>
          <Col span={12}><Form.Item name="dealId" label="Deal"><Select allowClear options={lookups.dealOptions} /></Form.Item></Col>
        </Row>
        <Form.Item name="text" label="Text" rules={[{ required: true }]}><Input.TextArea rows={5} /></Form.Item>
      </Form>
    </Modal>
  );
}

function AgentsPage() {
  const [editing, setEditing] = useState<Agent | null>();
  const query = useQuery({ queryKey: ['agents'], queryFn: api.agents.list });

  const columns: ColumnsType<Agent> = [
    { title: 'Agent', dataIndex: 'name' },
    { title: 'Description', dataIndex: 'description' },
    { title: 'Active', dataIndex: 'isActive', render: (value) => <StatusTag value={value ? 'Active' : 'Inactive'} /> },
    { title: 'Updated', dataIndex: 'updatedAt', render: formatDate },
    { title: '', width: 72, render: (_, row) => <Button icon={<EditOutlined />} onClick={() => setEditing(row)} /> },
  ];

  return (
    <>
      <PageTitle title="Agents" extra={<Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing(null)}>New agent</Button>} />
      <Table rowKey="id" loading={query.isLoading} dataSource={query.data} columns={columns} />
      <AgentEditor open={editing !== undefined} agent={editing ?? undefined} onClose={() => setEditing(undefined)} />
    </>
  );
}

function AgentEditor({ open, agent, onClose }: { open: boolean; agent?: Agent; onClose: () => void }) {
  const [form] = Form.useForm<AgentInput>();
  const mutation = useNotifyMutation((values: AgentInput) => agent ? api.agents.update(agent.id, values) : api.agents.create(values), ['agents']);

  useEffect(() => {
    if (open) form.setFieldsValue(agent ?? { isActive: true });
  }, [agent, form, open]);

  return (
    <Modal open={open} title={agent ? 'Edit agent' : 'New agent'} onCancel={onClose} onOk={() => form.submit()} confirmLoading={mutation.isPending} destroyOnHidden>
      <Form form={form} layout="vertical" onFinish={(values) => mutation.mutate(values, { onSuccess: onClose })}>
        <Form.Item name="name" label="Name" rules={[{ required: true }]}><Input /></Form.Item>
        <Form.Item name="description" label="Description"><Input.TextArea rows={4} /></Form.Item>
        <Form.Item name="isActive" label="Active" valuePropName="checked"><Switch /></Form.Item>
      </Form>
    </Modal>
  );
}

function AgentActionsPage() {
  const [open, setOpen] = useState(false);
  const [viewing, setViewing] = useState<AgentAction | null>(null);
  const query = useQuery({ queryKey: ['agentActions'], queryFn: api.agentActions.list });
  const approve = useNotifyMutation(api.agentActions.approve, ['agentActions', 'approvals', 'dashboard']);
  const reject = useNotifyMutation(api.agentActions.reject, ['agentActions', 'approvals', 'dashboard']);
  const execute = useNotifyMutation(api.agentActions.execute, ['agentActions', 'activities', 'messages', 'tasks', 'deals', 'contacts', 'dashboard']);

  const columns: ColumnsType<AgentAction> = [
    { title: 'Action', dataIndex: 'actionType' },
    { title: 'Agent', dataIndex: 'agentName' },
    { title: 'Target', render: (_, row) => row.targetEntityType ? `${row.targetEntityType}${row.targetEntityId ? ` · ${row.targetEntityId.slice(0, 8)}` : ''}` : '—' },
    { title: 'Status', dataIndex: 'status', render: StatusTag },
    { title: 'Approval', dataIndex: 'requiresApproval', render: (value) => value ? <Tag color="gold">Required</Tag> : <Tag>Auto</Tag> },
    { title: 'Created', dataIndex: 'createdAt', render: formatDate },
    {
      title: '',
      width: 216,
      render: (_, row) => (
        <Space>
          <Button icon={<EyeOutlined />} onClick={() => setViewing(row)} />
          <Button icon={<CheckOutlined />} disabled={!['Proposed', 'Failed'].includes(row.status)} onClick={() => approve.mutate(row.id)} />
          <Button icon={<CloseOutlined />} disabled={!['Proposed', 'Approved'].includes(row.status)} onClick={() => reject.mutate(row.id)} />
          <Button type="primary" disabled={!['Approved', 'Failed'].includes(row.status) && row.requiresApproval} onClick={() => execute.mutate(row.id)}>Run</Button>
        </Space>
      ),
    },
  ];

  return (
    <>
      <PageTitle title="Agent Actions" extra={<Button type="primary" icon={<PlusOutlined />} onClick={() => setOpen(true)}>New action</Button>} />
      <Table rowKey="id" loading={query.isLoading} dataSource={query.data} columns={columns} />
      <AgentActionEditor open={open} onClose={() => setOpen(false)} />
      <Drawer open={Boolean(viewing)} title={viewing?.actionType} onClose={() => setViewing(null)} size="large">
        {viewing && (
          <Space orientation="vertical" size={16} style={{ width: '100%' }}>
            <Descriptions column={1} bordered size="small">
              <Descriptions.Item label="Status"><StatusTag value={viewing.status} /></Descriptions.Item>
              <Descriptions.Item label="Agent">{viewing.agentName ?? viewing.agentId}</Descriptions.Item>
              <Descriptions.Item label="Reasoning">{viewing.reasoningSummary ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Error">{viewing.errorMessage ?? '—'}</Descriptions.Item>
            </Descriptions>
            <Title level={5}>Input</Title><JsonBlock value={viewing.inputJson} />
            <Title level={5}>Before</Title><JsonBlock value={viewing.beforeJson} />
            <Title level={5}>After</Title><JsonBlock value={viewing.afterJson} />
          </Space>
        )}
      </Drawer>
    </>
  );
}

function AgentActionEditor({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [form] = Form.useForm<AgentActionInput>();
  const lookups = useLookups();
  const mutation = useNotifyMutation(api.agentActions.create, ['agentActions', 'approvals', 'dashboard']);

  useEffect(() => {
    if (open) {
      form.setFieldsValue({
        actionType: 'AddNote',
        requiresApproval: true,
        inputJson: JSON.stringify({ type: 'Note', title: 'Agent note', description: '' }, null, 2),
      });
    }
  }, [form, open]);

  return (
    <Modal open={open} title="New agent action" onCancel={onClose} onOk={() => form.submit()} confirmLoading={mutation.isPending} width={760} destroyOnHidden>
      <Form
        form={form}
        layout="vertical"
        onFinish={(values) => mutation.mutate(values, { onSuccess: onClose })}
      >
        <Row gutter={12}>
          <Col span={12}><Form.Item name="agentId" label="Agent" rules={[{ required: true }]}><Select options={lookups.agentOptions} /></Form.Item></Col>
          <Col span={12}><Form.Item name="actionType" label="Action" rules={[{ required: true }]}><Select options={enumOptions(actionTypes)} /></Form.Item></Col>
        </Row>
        <Row gutter={12}>
          <Col span={12}><Form.Item name="targetEntityType" label="Target type"><Select allowClear options={enumOptions(entityTypes)} /></Form.Item></Col>
          <Col span={12}><Form.Item name="targetEntityId" label="Target ID"><Input /></Form.Item></Col>
        </Row>
        <Form.Item name="reasoningSummary" label="Reasoning summary"><Input.TextArea rows={2} /></Form.Item>
        <Form.Item name="inputJson" label="Input JSON" rules={[{ required: true }, { validator: (_, value) => jsonSchema.safeParse(value).success ? Promise.resolve() : Promise.reject(new Error('JSON is invalid')) }]}>
          <Input.TextArea rows={8} className="code-input" />
        </Form.Item>
        <Form.Item name="requiresApproval" label="Requires approval" valuePropName="checked"><Switch /></Form.Item>
      </Form>
    </Modal>
  );
}

function ApprovalsPage() {
  const query = useQuery({ queryKey: ['approvals'], queryFn: api.approvals.list });
  const approve = useNotifyMutation(api.approvals.approve, ['approvals', 'dashboard']);
  const reject = useNotifyMutation(api.approvals.reject, ['approvals', 'dashboard']);

  const columns: ColumnsType<ApprovalRequest> = [
    { title: 'Title', dataIndex: 'title' },
    { title: 'Entity', render: (_, row) => `${row.entityType} · ${row.entityId.slice(0, 8)}` },
    { title: 'Description', dataIndex: 'description', ellipsis: true },
    { title: 'Status', dataIndex: 'status', render: StatusTag },
    { title: 'Created', dataIndex: 'createdAt', render: formatDate },
    {
      title: '',
      width: 132,
      render: (_, row) => (
        <Space>
          <Button icon={<CheckOutlined />} disabled={row.status !== 'Pending'} onClick={() => approve.mutate(row.id)} />
          <Button danger icon={<CloseOutlined />} disabled={row.status !== 'Pending'} onClick={() => reject.mutate(row.id)} />
        </Space>
      ),
    },
  ];

  return (
    <>
      <PageTitle title="Approvals" />
      <Table rowKey="id" loading={query.isLoading} dataSource={query.data} columns={columns} />
    </>
  );
}

export default function App() {
  const themeConfig = useMemo(() => ({
    token: {
      colorPrimary: '#2f6f5e',
      colorInfo: '#315f8f',
      colorSuccess: '#3f7f4f',
      colorWarning: '#a56a1e',
      borderRadius: 6,
      fontFamily: 'Aptos, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
    },
    algorithm: theme.defaultAlgorithm,
  }), []);

  return (
    <ConfigProvider theme={themeConfig}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/*"
          element={(
            <RequireAuth>
              <Shell />
            </RequireAuth>
          )}
        />
      </Routes>
    </ConfigProvider>
  );
}
