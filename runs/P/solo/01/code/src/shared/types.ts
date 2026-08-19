export type ToolStatus = 'available' | 'borrowed';

export interface Tool {
  id: string;
  name: string;
  inventoryNumber: string;
  condition: string;
  status: ToolStatus;
  borrowedBy: string | null;
  borrowedAt: string | null;
}
