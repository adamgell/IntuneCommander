import { useEffect } from 'react';
import { DataGrid } from '@mui/x-data-grid';
import type { GridColDef } from '@mui/x-data-grid';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import LinearProgress from '@mui/material/LinearProgress';
import RefreshIcon from '@mui/icons-material/Refresh';
import SyncIcon from '@mui/icons-material/Sync';
import DeleteSweepIcon from '@mui/icons-material/DeleteSweep';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import StorageIcon from '@mui/icons-material/Storage';
import { useCacheSyncStore } from '../../store/cacheSyncStore';

interface CacheRow {
  id: string;
  cacheKey: string;
  label: string;
  isCached: boolean;
  cachedAt: string | null;
  itemCount: number;
}

const columns: GridColDef<CacheRow>[] = [
  {
    field: 'label',
    headerName: 'Data Type',
    flex: 1.5,
    minWidth: 200,
    renderCell: (params) => (
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <StorageIcon sx={{ fontSize: 14, color: 'var(--text-muted)' }} />
        <span style={{ fontSize: 13 }}>{params.value}</span>
      </Box>
    ),
  },
  {
    field: 'cacheKey',
    headerName: 'Cache Key',
    flex: 1,
    minWidth: 160,
    renderCell: (params) => (
      <code style={{ fontSize: 11, color: 'var(--text-muted)', fontFamily: 'monospace' }}>
        {params.value}
      </code>
    ),
  },
  {
    field: 'isCached',
    headerName: 'Status',
    width: 120,
    renderCell: (params) => (
      <Chip
        icon={params.value
          ? <CheckCircleIcon sx={{ fontSize: '14px !important' }} />
          : <CancelIcon sx={{ fontSize: '14px !important' }} />
        }
        label={params.value ? 'Cached' : 'Empty'}
        size="small"
        variant="outlined"
        color={params.value ? 'success' : 'default'}
        sx={{ fontSize: 11, height: 24 }}
      />
    ),
  },
  {
    field: 'itemCount',
    headerName: 'Items',
    width: 90,
    type: 'number',
    renderCell: (params) => (
      <span style={{
        fontSize: 13,
        fontWeight: params.value > 0 ? 600 : 400,
        color: params.value > 0 ? 'var(--text-primary)' : 'var(--text-muted)',
      }}>
        {params.value}
      </span>
    ),
  },
  {
    field: 'cachedAt',
    headerName: 'Cached At',
    width: 180,
    valueFormatter: (value: string | null) =>
      value ? new Date(value).toLocaleString() : '—',
  },
];

export function CacheDevWorkspace() {
  const {
    cacheStatus,
    isSyncing,
    progress,
    lastResult,
    loadStatus,
    syncAll,
    invalidateCache,
  } = useCacheSyncStore();

  useEffect(() => {
    loadStatus();
  }, [loadStatus]);

  const rows: CacheRow[] = cacheStatus.map((s) => ({
    id: s.cacheKey,
    ...s,
  }));

  const totalCached = cacheStatus.filter((s) => s.isCached).length;
  const totalItems = cacheStatus.reduce((sum, s) => sum + s.itemCount, 0);

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Cache Inspector</strong>
          <div className="workspace-stats">
            <span className="inline-stat">
              <strong>{totalCached}</strong> / {cacheStatus.length} types cached
            </span>
            <span className="inline-stat">
              <strong>{totalItems.toLocaleString()}</strong> total items
            </span>
          </div>
        </div>
      </div>

      {/* Action buttons */}
      <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827', p: 2, mx: 2 }}>
        <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'center', flexWrap: 'wrap' }}>
          <Button
            variant="outlined"
            size="small"
            startIcon={<RefreshIcon />}
            onClick={() => loadStatus()}
            sx={{ textTransform: 'none', color: 'var(--text-secondary)', borderColor: 'var(--border)' }}
          >
            Refresh Status
          </Button>
          <Button
            variant="contained"
            size="small"
            startIcon={<SyncIcon />}
            onClick={async () => { await syncAll(); loadStatus(); }}
            disabled={isSyncing}
            sx={{ textTransform: 'none' }}
          >
            {isSyncing ? 'Syncing...' : 'Sync All'}
          </Button>
          <Button
            variant="outlined"
            size="small"
            color="error"
            startIcon={<DeleteSweepIcon />}
            onClick={async () => { await invalidateCache(); loadStatus(); }}
            sx={{ textTransform: 'none' }}
          >
            Invalidate All
          </Button>

          {progress && isSyncing && (
            <Box sx={{ flex: 1, minWidth: 200 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                <Typography variant="caption" sx={{ color: 'var(--text-muted)' }}>
                  {progress.label}
                </Typography>
                <Typography variant="caption" sx={{ color: 'var(--text-muted)' }}>
                  {progress.current} / {progress.total}
                </Typography>
              </Box>
              <LinearProgress
                variant="determinate"
                value={progress.total > 0 ? (progress.current / progress.total) * 100 : 0}
                sx={{ height: 6, borderRadius: 3 }}
              />
            </Box>
          )}
        </Box>

        {lastResult && !isSyncing && (
          <Box sx={{ mt: 1.5, display: 'flex', gap: 1.5 }}>
            <Chip
              label={`${lastResult.successCount} succeeded`}
              size="small"
              color="success"
              variant="outlined"
              sx={{ fontSize: 11 }}
            />
            {lastResult.errorCount > 0 && (
              <Chip
                label={`${lastResult.errorCount} failed`}
                size="small"
                color="error"
                variant="outlined"
                sx={{ fontSize: 11 }}
              />
            )}
          </Box>
        )}
      </Paper>

      {/* Cache data grid */}
      <Box sx={{ mx: 2, mt: 2 }}>
        <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827' }}>
          <DataGrid
            rows={rows}
            columns={columns}
            getRowHeight={() => 'auto'}
            pageSizeOptions={[25, 50]}
            initialState={{
              pagination: { paginationModel: { pageSize: 50 } },
              sorting: { sortModel: [{ field: 'label', sort: 'asc' }] },
            }}
            disableRowSelectionOnClick
            sx={{
              border: 'none',
              fontSize: 12,
              '& .MuiDataGrid-columnHeaders': {
                backgroundColor: '#0f172a',
                fontSize: 11,
                fontWeight: 600,
                letterSpacing: '0.06em',
                textTransform: 'uppercase',
                color: 'var(--text-muted)',
                borderBottom: '1px solid var(--border)',
              },
              '& .MuiDataGrid-columnHeader': {
                '&:focus, &:focus-within': { outline: 'none' },
              },
              '& .MuiDataGrid-cell': {
                borderBottom: '1px solid var(--border)',
                color: 'var(--text-secondary)',
                display: 'flex',
                alignItems: 'center',
                py: 1,
                '&:focus, &:focus-within': { outline: 'none' },
              },
              '& .MuiDataGrid-row:hover': { bgcolor: 'rgba(255,255,255,0.03)' },
              '& .MuiDataGrid-footerContainer': { borderTop: '1px solid var(--border)' },
              '& .MuiTablePagination-root': { color: 'var(--text-muted)' },
              '& .MuiDataGrid-scrollbarFiller--header': { bgcolor: '#0f172a' },
            }}
          />
        </Paper>
      </Box>

      <div className="workspace-footer">
        <span>{cacheStatus.length} data types</span>
      </div>
    </div>
  );
}
