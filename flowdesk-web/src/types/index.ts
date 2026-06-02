// ── Auth ──────────────────────────────────────────────────────────────────────
export interface AuthResponse {
  userId: string;
  name: string;
  email: string;
  role: "AgencyOwner" | "AgencyMember" | "Client";
  organisationName: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  organisationName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface InviteRequest {
  email: string;
  name: string;
  role: "AgencyMember" | "Client";
}

// ── Organisation ──────────────────────────────────────────────────────────────
export interface OrganisationResponse {
  id: string;
  name: string;
  slug: string;
  primaryColor: string | null;
  plan: string;
  createdAt: string;
}

export interface UpdateOrganisationRequest {
  name: string;
  primaryColor: string | null;
}

// ── Users ─────────────────────────────────────────────────────────────────────
export interface UserSummary {
  id: string;
  name: string;
  email: string;
}

// ── Projects ──────────────────────────────────────────────────────────────────
export interface Project {
  id: string;
  name: string;
  description: string | null;
  status: "Active" | "Paused" | "Completed";
  clientId: string;
  clientName: string;
  organisationId: string;
  createdAt: string;
}

export interface ProjectStatsResponse {
  progressPercent: number;
  milestoneCount: number;
  completedMilestones: number;
  deliverableCount: number;
  approvedDeliverables: number;
}

export interface CreateProjectRequest {
  name: string;
  description: string | null;
  clientId: string;
}

export interface UpdateProjectRequest {
  name: string;
  description: string | null;
  status: string;
}

// ── Milestones ────────────────────────────────────────────────────────────────
export interface Milestone {
  id: string;
  projectId: string;
  title: string;
  description: string | null;
  status: "Pending" | "InProgress" | "Completed";
  dueDate: string | null;
  order: number;
}

export interface CreateMilestoneRequest {
  title: string;
  description: string | null;
  order: number;
  dueDate: string | null;
}

export interface UpdateMilestoneStatusRequest {
  status: string;
}

// ── Deliverables ──────────────────────────────────────────────────────────────
export interface Deliverable {
  id: string;
  projectId: string;
  milestoneId: string | null;
  name: string;
  description: string | null;
  status: "Pending" | "UnderReview" | "Approved" | "Revision";
  fileUrl: string | null;
  version: number;
  revisionNotes: string | null;
  approvedAt: string | null;
  createdAt: string;
}

export interface CreateDeliverableRequest {
  name: string;
  description: string | null;
  milestoneId: string | null;
}

export interface UploadUrlResponse {
  uploadUrl: string;
  fileUrl: string;
}

// ── Invoices ──────────────────────────────────────────────────────────────────
export interface Invoice {
  id: string;
  organisationId: string;
  projectId: string | null;
  clientId: string;
  clientName: string;
  status: "Draft" | "Sent" | "Paid" | "Overdue";
  total: number;
  dueDate: string | null;
  createdAt: string;
}

// ── Errors ────────────────────────────────────────────────────────────────────
export interface ApiError {
  title: string;
  status: number;
  detail?: string;
}

// ── Messages ──────────────────────────────────────────────────────────────────
export interface Message {
  id: string;
  content: string;
  senderId: string;
  senderName: string;
  isRead: boolean;
  createdAt: string;
}

export interface CreateMessageRequest {
  content: string;
}
