export enum CloudEnvironment {
  Commercial = 'Commercial',
  GCC = 'GCC',
  GCCHigh = 'GCCHigh',
  DoD = 'DoD',
}

export enum AuthMethod {
  Interactive = 'Interactive',
  ClientSecret = 'ClientSecret',
  DeviceCode = 'DeviceCode',
}

export interface TenantProfile {
  id: string;
  name: string;
  tenantId: string;
  clientId: string;
  cloud: CloudEnvironment;
  authMethod: AuthMethod;
  clientSecret?: string;
  lastUsed?: string;
}

export interface NavCategory {
  name: string;
  icon: string;
}

export interface NavGroup {
  name: string;
  icon: string;
  children: NavCategory[];
}

export interface ShellState {
  isConnected: boolean;
  isBusy: boolean;
  statusText: string;
  errorMessage?: string | null;
  activeProfile?: TenantProfile | null;
}

export interface DeviceCodeInfo {
  userCode: string;
  verificationUrl: string;
  message: string;
}

export interface ProfilesPayload {
  profiles: TenantProfile[];
  activeProfileId: string | null;
}

export interface ImportResult extends ProfilesPayload {
  imported: number;
}

// Navigation types for two-tier top nav
export interface PrimaryNavTab {
  id: string;
  label: string;
  secondaryTabs: SecondaryNavTab[];
}

export interface SecondaryNavTab {
  id: string;
  label: string;
}

export interface SidebarItem {
  id: string;
  label: string;
  count?: number;
}

export interface SidebarSection {
  label: string;
  items: SidebarItem[];
}

// Navigation definitions
export const primaryNavTabs: PrimaryNavTab[] = [
  {
    id: 'configuration',
    label: 'Configuration',
    secondaryTabs: [
      { id: 'settings-catalog', label: 'Settings Catalog' },
    ],
  },
  {
    id: 'applications',
    label: 'Applications',
    secondaryTabs: [
      { id: 'applications', label: 'App Gallery' },
    ],
  },
  {
    id: 'security',
    label: 'Security',
    secondaryTabs: [
      { id: 'conditional-access', label: 'Conditional Access' },
    ],
  },
  {
    id: 'devices',
    label: 'Devices',
    secondaryTabs: [
      { id: 'detection-remediation', label: 'Detection & Remediation' },
    ],
  },
  {
    id: 'operations',
    label: 'Operations',
    secondaryTabs: [
      { id: 'assignment-explorer', label: 'Assignment Explorer' },
    ],
  },
];

// Sidebar items per primary tab
export const sidebarByTab: Record<string, SidebarSection[]> = {
  configuration: [
    {
      label: 'Workspaces',
      items: [
        { id: 'overview', label: 'Overview' },
        { id: 'settings-catalog', label: 'Settings Catalog' },
      ],
    },
  ],
  applications: [
    {
      label: 'Workspaces',
      items: [
        { id: 'applications', label: 'App Gallery' },
      ],
    },
  ],
  security: [
    {
      label: 'Workspaces',
      items: [
        { id: 'security-posture', label: 'Security Posture' },
        { id: 'conditional-access', label: 'Conditional Access' },
      ],
    },
  ],
  devices: [
    {
      label: 'Workspaces',
      items: [
        { id: 'detection-remediation', label: 'Detection & Remediation' },
        { id: 'scripts-hub', label: 'Scripts Hub' },
      ],
    },
  ],
  operations: [
    {
      label: 'Workspaces',
      items: [
        { id: 'assignment-explorer', label: 'Assignment Explorer' },
        { id: 'policy-comparison', label: 'Policy Diff' },
      ],
    },
  ],
};
