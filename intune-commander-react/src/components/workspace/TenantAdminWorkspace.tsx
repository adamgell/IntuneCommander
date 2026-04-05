import { useEffect } from 'react';
import {
  DataGrid,
  type GridColDef,
  type GridRowParams,
  Toolbar,
  QuickFilter,
  QuickFilterControl,
  QuickFilterClear,
} from '@mui/x-data-grid';
import { useTenantAdminStore } from '../../store/tenantAdminStore';
import { ENTITY_TYPE_LABELS, type TenantAdminEntityType, type TenantAdminListItem } from '../../types/tenantAdmin';
import '../../styles/workspace.css';

const entityTypes = Object.keys(ENTITY_TYPE_LABELS) as TenantAdminEntityType[];

const columns: GridColDef<TenantAdminListItem>[] = [
  {
    field: 'displayName',
    headerName: 'Name',
    flex: 2,
    minWidth: 220,
    renderCell: (params) => (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 2, lineHeight: 1.4, padding: '6px 0', whiteSpace: 'normal', wordBreak: 'break-word' }}>
        <strong style={{ fontSize: 13, color: 'var(--text-primary)' }}>{params.row.displayName}</strong>
        {params.row.description && (
          <span style={{ fontSize: 11, color: 'var(--text-tertiary)', lineHeight: 1.3, overflow: 'hidden', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical' }}>
            {params.row.description}
          </span>
        )}
      </div>
    ),
  },
  {
    field: 'lastModifiedDateTime',
    headerName: 'Last Modified',
    width: 170,
    renderCell: (params) => {
      const dt = params.value as string | undefined;
      if (!dt) return <span style={{ color: 'var(--text-tertiary)' }}>--</span>;
      return <span style={{ fontSize: 12 }}>{new Date(dt).toLocaleDateString()}</span>;
    },
  },
];

function CustomToolbar() {
  return (
    <Toolbar>
      <QuickFilter>
        <QuickFilterControl />
        <QuickFilterClear />
      </QuickFilter>
    </Toolbar>
  );
}

export function TenantAdminWorkspace() {
  const {
    activeEntityType, items, selectedId, detail,
    isLoadingList, isLoadingDetail, hasAttemptedLoad, error,
    setEntityType, loadItems, selectItem, clearSelection,
  } = useTenantAdminStore();

  useEffect(() => {
    if (!hasAttemptedLoad) loadItems();
  }, [activeEntityType, hasAttemptedLoad, loadItems]);

  const hasDetail = selectedId !== null;

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Tenant Admin</strong>
          <div className="workspace-stats">
            <span className="inline-stat">
              {items.length} {ENTITY_TYPE_LABELS[activeEntityType]}
            </span>
          </div>
        </div>
      </div>

      <div style={{ display: 'flex', flex: 1, overflow: 'hidden' }}>
        {/* Entity Type Sidebar */}
        <div style={{
          width: 180,
          borderRight: '1px solid var(--border)',
          overflow: 'auto',
          flexShrink: 0,
        }}>
          {entityTypes.map(type => (
            <button
              key={type}
              onClick={() => setEntityType(type)}
              style={{
                display: 'block',
                width: '100%',
                padding: '8px 12px',
                border: 'none',
                background: activeEntityType === type ? 'var(--surface-secondary)' : 'transparent',
                borderLeft: activeEntityType === type ? '2px solid var(--brand)' : '2px solid transparent',
                color: activeEntityType === type ? 'var(--text-primary)' : 'var(--text-secondary)',
                fontSize: 12,
                textAlign: 'left',
                cursor: 'pointer',
                fontWeight: activeEntityType === type ? 600 : 400,
              }}
            >
              {ENTITY_TYPE_LABELS[type]}
            </button>
          ))}
        </div>

        {/* List + Detail */}
        <div className={`settings-columns${hasDetail ? ' detail-active' : ''}`} style={{ flex: 1 }}>
          {/* List Panel */}
          <div className="panel" style={{ minWidth: 0 }}>
            {error && (
              <div style={{ color: 'var(--danger, #ef4444)', fontSize: 13, padding: '8px 12px' }}>
                {error}
              </div>
            )}
            <DataGrid<TenantAdminListItem>
              rows={items}
              columns={columns}
              loading={isLoadingList}
              getRowId={(row) => row.id}
              getRowHeight={() => 'auto'}
              onRowClick={(params: GridRowParams<TenantAdminListItem>) => selectItem(params.row.id)}
              rowSelection={false}
              slots={{ toolbar: CustomToolbar }}
              getRowClassName={(params) =>
                params.row.id === selectedId ? 'row-selected' : ''
              }
              sx={{
                border: 'none',
                '& .MuiDataGrid-cell': { py: 0.5 },
                '& .row-selected': {
                  backgroundColor: 'var(--surface-secondary) !important',
                },
              }}
            />
          </div>

          {/* Detail Panel */}
          {hasDetail && (
            <div className="panel detail-panel" style={{ minWidth: 320, maxWidth: 480 }}>
              <div className="panel-header" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <strong style={{ fontSize: 13 }}>Details</strong>
                <button className="btn btn-ghost" style={{ fontSize: 11, padding: '2px 8px' }} onClick={clearSelection}>Close</button>
              </div>
              <div className="panel-body" style={{ overflow: 'auto', padding: 12 }}>
                {isLoadingDetail ? (
                  <div style={{ color: 'var(--text-tertiary)', fontSize: 13, textAlign: 'center', padding: 24 }}>
                    Loading...
                  </div>
                ) : detail ? (
                  <DetailView data={detail} />
                ) : null}
              </div>
            </div>
          )}
        </div>
      </div>

      <div className="workspace-footer" />
    </div>
  );
}

function DetailView({ data }: { data: Record<string, unknown> }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
      {Object.entries(data).map(([key, value]) => {
        if (key === 'id') return null;
        return (
          <div key={key}>
            <div style={{ fontSize: 10, color: 'var(--text-tertiary)', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: 2 }}>
              {formatLabel(key)}
            </div>
            <div style={{ fontSize: 13, color: 'var(--text-primary)', wordBreak: 'break-word' }}>
              {formatDetailValue(value)}
            </div>
          </div>
        );
      })}
    </div>
  );
}

function formatLabel(key: string): string {
  return key.replace(/([A-Z])/g, ' $1').replace(/^./, s => s.toUpperCase()).trim();
}

function formatDetailValue(value: unknown): string {
  if (value === null || value === undefined) return '--';
  if (typeof value === 'boolean') return value ? 'Yes' : 'No';
  if (Array.isArray(value)) return value.length === 0 ? '(none)' : value.join(', ');
  if (typeof value === 'object') return JSON.stringify(value, null, 2);
  return String(value);
}
