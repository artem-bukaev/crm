export type ContactStatus = 'Lead' | 'Active' | 'Inactive' | 'Archived';
export type DealStatus = 'Open' | 'Won' | 'Lost' | 'Canceled';
export type TaskStatus = 'New' | 'InProgress' | 'Completed' | 'Canceled';
export type TaskPriority = 'Low' | 'Normal' | 'High' | 'Urgent';
export type ActivityType = 'Note' | 'Call' | 'Email' | 'TelegramMessage' | 'Meeting' | 'SystemEvent' | 'AgentAction';
export type MessageChannel = 'Email' | 'Telegram' | 'WhatsApp' | 'LinkedIn' | 'WebsiteChat' | 'Manual';
export type MessageDirection = 'Incoming' | 'Outgoing';
export type ConversationStatus = 'Unread' | 'WaitingOnUs' | 'WaitingOnThem' | 'Closed';
export type WorkQueueItemType = 'Task' | 'Activity';
export type WorkQueueBucket = 'Overdue' | 'DueToday' | 'ThisWeek' | 'Upcoming' | 'Unassigned';
export type AgentActionType =
  | 'CreateContact'
  | 'UpdateContact'
  | 'CreateDeal'
  | 'UpdateDealStage'
  | 'CreateTask'
  | 'CompleteTask'
  | 'AddNote'
  | 'DraftMessage'
  | 'SendMessage'
  | 'RequestHumanApproval';
export type AgentActionStatus = 'Proposed' | 'Approved' | 'Rejected' | 'Executed' | 'Failed' | 'Canceled';
export type EntityType =
  | 'Contact'
  | 'Company'
  | 'Pipeline'
  | 'PipelineStage'
  | 'Deal'
  | 'Task'
  | 'Activity'
  | 'Message'
  | 'Agent'
  | 'AgentAction'
  | 'ApprovalRequest';
export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected' | 'Canceled';

export type DashboardSummary = {
  contacts: number;
  companies: number;
  openDeals: number;
  openDealAmount: number;
  openTasks: number;
  pendingAgentActions: number;
  pendingApprovals: number;
};

export type Contact = {
  id: string;
  firstName?: string | null;
  lastName?: string | null;
  middleName?: string | null;
  fullName: string;
  phone?: string | null;
  email?: string | null;
  telegramUsername?: string | null;
  companyId?: string | null;
  companyName?: string | null;
  position?: string | null;
  source?: string | null;
  status: ContactStatus;
  createdAt: string;
  updatedAt: string;
};

export type Company = {
  id: string;
  name: string;
  legalName?: string | null;
  inn?: string | null;
  website?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type Pipeline = {
  id: string;
  name: string;
  description?: string | null;
  isDefault: boolean;
  createdAt: string;
  updatedAt: string;
};

export type PipelineStage = {
  id: string;
  pipelineId: string;
  name: string;
  sortOrder: number;
  probability: number;
  isWon: boolean;
  isLost: boolean;
  createdAt: string;
  updatedAt: string;
};

export type Deal = {
  id: string;
  title: string;
  contactId?: string | null;
  contactName?: string | null;
  companyId?: string | null;
  companyName?: string | null;
  pipelineId: string;
  pipelineName?: string | null;
  stageId: string;
  stageName?: string | null;
  amount: number;
  currency: string;
  probability: number;
  status: DealStatus;
  source?: string | null;
  responsibleUserId?: string | null;
  createdAt: string;
  updatedAt: string;
  closedAt?: string | null;
};

export type CrmTask = {
  id: string;
  title: string;
  description?: string | null;
  dueAt?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  contactId?: string | null;
  contactName?: string | null;
  companyId?: string | null;
  companyName?: string | null;
  dealId?: string | null;
  dealTitle?: string | null;
  responsibleUserId?: string | null;
  createdAt: string;
  updatedAt: string;
  completedAt?: string | null;
};

export type Activity = {
  id: string;
  type: ActivityType;
  title: string;
  description?: string | null;
  contactId?: string | null;
  contactName?: string | null;
  companyId?: string | null;
  companyName?: string | null;
  dealId?: string | null;
  dealTitle?: string | null;
  createdByUserId?: string | null;
  createdByAgentId?: string | null;
  createdAt: string;
};

export type Message = {
  id: string;
  channel: MessageChannel;
  direction: MessageDirection;
  externalMessageId?: string | null;
  contactId?: string | null;
  contactName?: string | null;
  dealId?: string | null;
  dealTitle?: string | null;
  text: string;
  receivedAt?: string | null;
  sentAt?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type Conversation = {
  id: string;
  contactId?: string | null;
  contactName?: string | null;
  companyId?: string | null;
  companyName?: string | null;
  dealId?: string | null;
  dealTitle?: string | null;
  lastChannel: MessageChannel;
  lastDirection: MessageDirection;
  lastMessageText: string;
  lastMessageAt: string;
  status: ConversationStatus;
  messageCount: number;
  openTaskCount: number;
  messages: Message[];
};

export type WorkQueueItem = {
  id: string;
  type: WorkQueueItemType;
  sourceId: string;
  title: string;
  description?: string | null;
  activityType?: ActivityType | null;
  taskStatus?: TaskStatus | null;
  priority?: TaskPriority | null;
  dueAt?: string | null;
  startedAt?: string | null;
  contactId?: string | null;
  contactName?: string | null;
  companyId?: string | null;
  companyName?: string | null;
  dealId?: string | null;
  dealTitle?: string | null;
  responsibleUserId?: string | null;
  bucket: WorkQueueBucket;
  isOverdue: boolean;
  sortAt: string;
};

export type ContactDuplicateCandidate = {
  id: string;
  primaryContact: Contact;
  duplicateContact: Contact;
  confidence: number;
  reason: string;
};

export type MergeContactsInput = {
  primaryContactId: string;
  duplicateContactId: string;
};

export type BulkCreateTaskInput = {
  contactIds?: string[];
  dealIds?: string[];
  title: string;
  description?: string | null;
  dueAt?: string | null;
  priority: TaskPriority;
  responsibleUserId?: string | null;
};

export type BulkOperationItemResult = {
  targetId: string;
  targetType: EntityType;
  succeeded: boolean;
  message?: string | null;
  createdTaskId?: string | null;
};

export type BulkOperationResult = {
  requested: number;
  succeeded: number;
  failed: number;
  items: BulkOperationItemResult[];
};

export type Agent = {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

export type AgentAction = {
  id: string;
  agentId: string;
  agentName?: string | null;
  actionType: AgentActionType;
  status: AgentActionStatus;
  targetEntityType?: EntityType | null;
  targetEntityId?: string | null;
  inputJson: string;
  reasoningSummary?: string | null;
  beforeJson?: string | null;
  afterJson?: string | null;
  requiresApproval: boolean;
  approvedByUserId?: string | null;
  approvedAt?: string | null;
  rejectedByUserId?: string | null;
  rejectedAt?: string | null;
  errorMessage?: string | null;
  createdAt: string;
  updatedAt: string;
  executedAt?: string | null;
};

export type ApprovalRequest = {
  id: string;
  entityType: EntityType;
  entityId: string;
  title: string;
  description?: string | null;
  status: ApprovalStatus;
  requestedByUserId?: string | null;
  requestedByAgentId?: string | null;
  approvedByUserId?: string | null;
  approvedAt?: string | null;
  rejectedByUserId?: string | null;
  rejectedAt?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type ContactInput = Omit<Contact, 'id' | 'fullName' | 'companyName' | 'createdAt' | 'updatedAt'>;
export type CompanyInput = Omit<Company, 'id' | 'createdAt' | 'updatedAt'>;
export type DealInput = Omit<Deal, 'id' | 'contactName' | 'companyName' | 'pipelineName' | 'stageName' | 'probability' | 'createdAt' | 'updatedAt' | 'closedAt'> & {
  probability?: number | null;
};
export type TaskInput = Omit<CrmTask, 'id' | 'contactName' | 'companyName' | 'dealTitle' | 'createdAt' | 'updatedAt' | 'completedAt'>;
export type ActivityInput = Omit<Activity, 'id' | 'contactName' | 'companyName' | 'dealTitle' | 'createdAt'>;
export type MessageInput = Omit<Message, 'id' | 'contactName' | 'dealTitle' | 'createdAt' | 'updatedAt'>;
export type AgentInput = Omit<Agent, 'id' | 'createdAt' | 'updatedAt'>;
export type AgentActionInput = Pick<AgentAction, 'agentId' | 'actionType' | 'targetEntityType' | 'targetEntityId' | 'inputJson' | 'reasoningSummary' | 'requiresApproval'>;

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '');

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const data = text ? JSON.parse(text) : undefined;

  if (!response.ok) {
    const message = data?.error?.message ?? response.statusText;
    throw new Error(message);
  }

  return data as T;
}

const json = (body: unknown): RequestInit => ({
  method: 'POST',
  body: JSON.stringify(body),
});

const put = (body: unknown): RequestInit => ({
  method: 'PUT',
  body: JSON.stringify(body),
});

export const api = {
  baseUrl: apiBaseUrl || 'same origin',
  dashboard: () => request<DashboardSummary>('/api/dashboard'),
  contacts: {
    list: () => request<Contact[]>('/api/contacts'),
    duplicates: () => request<ContactDuplicateCandidate[]>('/api/contacts/duplicates'),
    create: (body: ContactInput) => request<Contact>('/api/contacts', json(body)),
    update: (id: string, body: ContactInput) => request<Contact>(`/api/contacts/${id}`, put(body)),
    merge: (body: MergeContactsInput) => request<Contact>('/api/contacts/merge', json(body)),
    bulkCreateTask: (body: BulkCreateTaskInput) => request<BulkOperationResult>('/api/contacts/bulk/create-task', json(body)),
    delete: (id: string) => request<void>(`/api/contacts/${id}`, { method: 'DELETE' }),
  },
  companies: {
    list: () => request<Company[]>('/api/companies'),
    create: (body: CompanyInput) => request<Company>('/api/companies', json(body)),
    update: (id: string, body: CompanyInput) => request<Company>(`/api/companies/${id}`, put(body)),
    delete: (id: string) => request<void>(`/api/companies/${id}`, { method: 'DELETE' }),
  },
  pipelines: {
    list: () => request<Pipeline[]>('/api/pipelines'),
    stages: (pipelineId: string) => request<PipelineStage[]>(`/api/pipelines/${pipelineId}/stages`),
  },
  deals: {
    list: () => request<Deal[]>('/api/deals'),
    create: (body: DealInput) => request<Deal>('/api/deals', json(body)),
    update: (id: string, body: DealInput) => request<Deal>(`/api/deals/${id}`, put(body)),
    moveStage: (id: string, stageId: string) => request<Deal>(`/api/deals/${id}/move-stage`, json({ stageId })),
    markWon: (id: string) => request<Deal>(`/api/deals/${id}/mark-won`, { method: 'POST' }),
    markLost: (id: string) => request<Deal>(`/api/deals/${id}/mark-lost`, { method: 'POST' }),
    bulkCreateTask: (body: BulkCreateTaskInput) => request<BulkOperationResult>('/api/deals/bulk/create-task', json(body)),
    delete: (id: string) => request<void>(`/api/deals/${id}`, { method: 'DELETE' }),
  },
  tasks: {
    list: (status?: TaskStatus) => request<CrmTask[]>(`/api/tasks${status ? `?status=${status}` : ''}`),
    create: (body: TaskInput) => request<CrmTask>('/api/tasks', json(body)),
    update: (id: string, body: TaskInput) => request<CrmTask>(`/api/tasks/${id}`, put(body)),
    complete: (id: string) => request<CrmTask>(`/api/tasks/${id}/complete`, { method: 'POST' }),
    cancel: (id: string) => request<CrmTask>(`/api/tasks/${id}/cancel`, { method: 'POST' }),
    delete: (id: string) => request<void>(`/api/tasks/${id}`, { method: 'DELETE' }),
  },
  activities: {
    list: () => request<Activity[]>('/api/activities/timeline'),
    workQueue: () => request<WorkQueueItem[]>('/api/activities/work-queue'),
    create: (body: ActivityInput) => request<Activity>('/api/activities', json(body)),
  },
  messages: {
    list: () => request<Message[]>('/api/messages'),
    conversations: () => request<Conversation[]>('/api/messages/conversations'),
    create: (body: MessageInput) => request<Message>('/api/messages', json(body)),
  },
  agents: {
    list: () => request<Agent[]>('/api/agents'),
    create: (body: AgentInput) => request<Agent>('/api/agents', json(body)),
    update: (id: string, body: AgentInput) => request<Agent>(`/api/agents/${id}`, put(body)),
  },
  agentActions: {
    list: () => request<AgentAction[]>('/api/agent-actions'),
    create: (body: AgentActionInput) => request<AgentAction>('/api/agent-actions', json(body)),
    approve: (id: string) => request<AgentAction>(`/api/agent-actions/${id}/approve`, json({})),
    reject: (id: string) => request<AgentAction>(`/api/agent-actions/${id}/reject`, json({})),
    execute: (id: string) => request<AgentAction>(`/api/agent-actions/${id}/execute`, { method: 'POST' }),
  },
  approvals: {
    list: () => request<ApprovalRequest[]>('/api/approvals'),
    approve: (id: string) => request<ApprovalRequest>(`/api/approvals/${id}/approve`, json({})),
    reject: (id: string) => request<ApprovalRequest>(`/api/approvals/${id}/reject`, json({})),
  },
};
