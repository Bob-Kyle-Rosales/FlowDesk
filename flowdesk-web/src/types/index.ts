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

export interface Project {
  id: string;
  name: string;
  description: string;
  status: "Active" | "Paused" | "Completed";
  clientId: string;
  clientName: string;
  organisationId: string;
  createdAt: string;
  updatedAt: string;
}

export interface Milestone {
  id: string;
  projectId: string;
  title: string;
  description: string;
  status: "Pending" | "InProgress" | "Completed";
  dueDate: string | null;
  order: number;
}

export interface Deliverable {
  id: string;
  projectId: string;
  milestoneId: string | null;
  title: string;
  description: string;
  fileUrl: string | null;
  status: "Pending" | "UnderReview" | "Approved" | "Revision";
  version: number;
  revisionNotes: string | null;
}

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

export interface ApiError {
  title: string;
  status: number;
  detail?: string;
}
