export type TenantAdminEntityType =
  | 'scopeTags'
  | 'roles'
  | 'intuneBranding'
  | 'azureBranding'
  | 'termsAndConditions'
  | 'termsOfUse'
  | 'admxFiles'
  | 'reusableSettings'
  | 'notifications'
  | 'policySets';

export interface TenantAdminListItem {
  id: string;
  displayName: string;
  description?: string;
  entityType: string;
  lastModifiedDateTime?: string;
}

export const ENTITY_TYPE_LABELS: Record<TenantAdminEntityType, string> = {
  scopeTags: 'Scope Tags',
  roles: 'Roles',
  intuneBranding: 'Intune Branding',
  azureBranding: 'Azure Branding',
  termsAndConditions: 'Terms & Conditions',
  termsOfUse: 'Terms of Use',
  admxFiles: 'ADMX Files',
  reusableSettings: 'Reusable Settings',
  notifications: 'Notifications',
  policySets: 'Policy Sets',
};
