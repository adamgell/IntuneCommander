import { useState, useCallback } from 'react';
import { DataGrid } from '@mui/x-data-grid';
import type { GridColDef, GridRowId } from '@mui/x-data-grid';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import Chip from '@mui/material/Chip';
import IconButton from '@mui/material/IconButton';
import CircularProgress from '@mui/material/CircularProgress';
import LinearProgress from '@mui/material/LinearProgress';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import SearchIcon from '@mui/icons-material/Search';
import RocketLaunchIcon from '@mui/icons-material/RocketLaunch';
import StopIcon from '@mui/icons-material/Stop';
import RefreshIcon from '@mui/icons-material/Refresh';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import { useDetectionRemediationStore } from '../../store/detectionRemediationStore';
import type { DeviceSearchResult, DeploymentRecord, DeviceRunState } from '../../types/detectionRemediation';

const dgSx = {
  border: 'none',
  '& .MuiDataGrid-columnHeaders': { bgcolor: '#0f172a' },
  '& .MuiDataGrid-columnHeaderTitle': {
    fontSize: 11, fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase',
    color: 'var(--text-muted)',
  },
  '& .MuiDataGrid-cell': {
    borderBottom: '1px solid var(--border)', color: 'var(--text-secondary)', fontSize: 12,
    py: 1.5,
  },
  '& .MuiDataGrid-row:hover': { bgcolor: 'rgba(255,255,255,0.03)' },
  '& .MuiDataGrid-footerContainer': { borderTop: '1px solid var(--border)' },
  '& .MuiTablePagination-root': { color: 'var(--text-muted)' },
  '& .MuiDataGrid-toolbarContainer': { px: 1, py: 0.5 },
  '& .MuiDataGrid-scrollbarFiller--header': { bgcolor: '#0f172a' },
};

/* ── Device search columns ─────────────────────────────── */

const deviceColumns: GridColDef<DeviceSearchResult>[] = [
  { field: 'deviceName', headerName: 'DEVICE NAME', flex: 1.5, minWidth: 180 },
  { field: 'operatingSystem', headerName: 'OS', width: 100 },
  { field: 'osVersion', headerName: 'OS VERSION', flex: 1, minWidth: 120 },
  { field: 'model', headerName: 'MODEL', flex: 1, minWidth: 120 },
  { field: 'manufacturer', headerName: 'MANUFACTURER', flex: 1, minWidth: 120 },
  {
    field: 'lastSyncDateTime',
    headerName: 'LAST SYNC',
    width: 160,
    valueFormatter: (value: string) => value ? new Date(value).toLocaleString() : '—',
  },
];

/* ── Deployment results columns ──────────────────────── */

const deployColumns: GridColDef<DeploymentRecord>[] = [
  { field: 'deviceName', headerName: 'DEVICE', flex: 1.5, minWidth: 180 },
  {
    field: 'succeeded',
    headerName: 'STATUS',
    width: 130,
    renderCell: (params) => (
      <Chip
        label={params.value ? 'Dispatched' : 'Failed'}
        size="small"
        color={params.value ? 'success' : 'error'}
        variant="outlined"
        icon={params.value ? <CheckCircleIcon /> : <ErrorOutlineIcon />}
      />
    ),
  },
  { field: 'errorMessage', headerName: 'ERROR', flex: 2, minWidth: 200 },
  {
    field: 'dispatchedAt',
    headerName: 'DISPATCHED',
    width: 180,
    valueFormatter: (value: string) => value ? new Date(value).toLocaleString() : '—',
  },
];

/* ── Monitoring columns ──────────────────────────────── */

const monitorColumns: GridColDef<DeviceRunState>[] = [
  {
    field: 'deviceName',
    headerName: 'DEVICE',
    flex: 1.2,
    minWidth: 180,
    renderCell: (params) => (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 1, lineHeight: 1.4, padding: '4px 0' }}>
        <strong style={{ fontSize: 12 }}>{params.row.deviceName}</strong>
        {params.row.userPrincipalName && (
          <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>{params.row.userPrincipalName}</span>
        )}
      </div>
    ),
  },
  {
    field: 'detectionState',
    headerName: 'DETECTION',
    width: 130,
    renderCell: (params) => {
      const color = params.value === 'NotApplicable' ? 'default'
        : params.value === 'Success' ? 'success'
        : params.value === 'ScriptError' ? 'error'
        : 'warning';
      return <Chip label={params.value} size="small" color={color} variant="outlined" />;
    },
  },
  {
    field: 'remediationState',
    headerName: 'REMEDIATION',
    width: 140,
    renderCell: (params) => {
      const color = params.value === 'NotApplicable' ? 'default'
        : params.value === 'Success' || params.value === 'Remediable' ? 'success'
        : params.value === 'ScriptError' ? 'error'
        : 'warning';
      return <Chip label={params.value} size="small" color={color} variant="outlined" />;
    },
  },
  {
    field: 'preRemediationDetectionScriptOutput',
    headerName: 'PRE-DETECT OUTPUT',
    flex: 1,
    minWidth: 150,
  },
  {
    field: 'lastStateUpdateDateTime',
    headerName: 'UPDATED',
    width: 170,
    valueFormatter: (value: string) => value ? new Date(value).toLocaleString() : '—',
  },
];

/* ── Main DeployPanel ────────────────────────────────── */

interface DeployPanelProps {
  scriptId: string;
  scriptName: string;
  onBack: () => void;
}

export function DeployPanel({ scriptId, scriptName, onBack }: DeployPanelProps) {
  const {
    deviceSearchResults,
    isSearchingDevices,
    deploymentRecords,
    isDeploying,
    monitoringStates,
    isMonitoring,
    lastMonitorRefresh,
    searchDevices,
    deployToDevices,
    stopMonitoring,
    refreshMonitoring,
    resetDeployState,
  } = useDetectionRemediationStore();

  const [searchQuery, setSearchQuery] = useState('');
  const [selectedIds, setSelectedIds] = useState<GridRowId[]>([]);

  const hasDeployed = deploymentRecords.length > 0;

  const handleSearch = useCallback(() => {
    searchDevices(searchQuery);
  }, [searchQuery, searchDevices]);

  const handleDeploy = useCallback(() => {
    const idSet = new Set(selectedIds.map(String));
    const devices = deviceSearchResults
      .filter((d) => idSet.has(d.id))
      .map((d) => ({ id: d.id, deviceName: d.deviceName }));
    if (devices.length > 0) {
      deployToDevices(scriptId, devices);
    }
  }, [selectedIds, deviceSearchResults, scriptId, deployToDevices]);

  const handleBack = useCallback(() => {
    resetDeployState();
    onBack();
  }, [resetDeployState, onBack]);

  const handleSelectionChange = useCallback((model: { type: string; ids: Set<GridRowId> }) => {
    setSelectedIds(Array.from(model.ids));
  }, []);

  const selectedCount = selectedIds.length;

  return (
    <Box sx={{ display: 'grid', gap: 2.5 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
        <IconButton onClick={handleBack} size="small" sx={{ color: 'var(--text-secondary)' }}>
          <ArrowBackIcon />
        </IconButton>
        <Box>
          <Typography variant="h6" sx={{ fontWeight: 600, lineHeight: 1.3 }}>
            Deploy on Demand
          </Typography>
          <Typography variant="body2" sx={{ color: 'var(--text-muted)', fontSize: 12 }}>
            {scriptName}
          </Typography>
        </Box>
      </Box>

      {!hasDeployed ? (
        <>
          {/* Search bar */}
          <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827', p: 2 }}>
            <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'center' }}>
              <TextField
                placeholder="Search devices by name..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                size="small"
                fullWidth
                sx={{
                  '& .MuiOutlinedInput-root': {
                    bgcolor: '#0f172a',
                    '& fieldset': { borderColor: 'var(--border)' },
                  },
                  '& .MuiOutlinedInput-input': { color: 'var(--text-primary)', fontSize: 13 },
                }}
              />
              <Button
                variant="contained"
                onClick={handleSearch}
                disabled={isSearchingDevices}
                startIcon={isSearchingDevices ? <CircularProgress size={16} /> : <SearchIcon />}
                sx={{ textTransform: 'none', whiteSpace: 'nowrap', minWidth: 100 }}
              >
                Search
              </Button>
            </Box>
            <Typography variant="caption" sx={{ color: 'var(--text-muted)', mt: 1, display: 'block' }}>
              Leave empty to load all cached Windows devices. Select devices to deploy to, then click Deploy.
            </Typography>
          </Paper>

          {/* Device search results */}
          {deviceSearchResults.length > 0 && (
            <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827' }}>
              <Box sx={{ p: 2, pb: 1, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  Devices ({deviceSearchResults.length})
                </Typography>
                {selectedCount > 0 && (
                  <Chip label={`${selectedCount} selected`} size="small" color="primary" variant="outlined" />
                )}
              </Box>
              <Box sx={{ height: 400 }}>
                <DataGrid
                  rows={deviceSearchResults}
                  columns={deviceColumns}
                  checkboxSelection
                  onRowSelectionModelChange={handleSelectionChange}
                  getRowHeight={() => 'auto'}
                  pageSizeOptions={[25, 50]}
                  initialState={{
                    pagination: { paginationModel: { pageSize: 25 } },
                  }}
                  sx={dgSx}
                />
              </Box>
              {/* Deploy button — inside the results panel so it's always visible */}
              {selectedCount > 0 && (
                <Box sx={{ display: 'flex', justifyContent: 'flex-end', p: 2, pt: 1.5 }}>
                  <Button
                    variant="contained"
                    color="warning"
                    size="large"
                    startIcon={isDeploying ? <CircularProgress size={20} color="inherit" /> : <RocketLaunchIcon />}
                    onClick={handleDeploy}
                    disabled={isDeploying}
                    sx={{ textTransform: 'none', px: 4 }}
                  >
                    {isDeploying ? 'Deploying...' : `Deploy to ${selectedCount} device(s)`}
                  </Button>
                </Box>
              )}
            </Paper>
          )}
        </>
      ) : (
        <>
          {/* Deployment results */}
          <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827' }}>
            <Box sx={{ p: 2, pb: 1 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                Deployment Results
              </Typography>
            </Box>
            <Box sx={{ height: 250 }}>
              <DataGrid
                rows={deploymentRecords}
                columns={deployColumns}
                getRowId={(r) => r.deviceId}
                getRowHeight={() => 'auto'}
                pageSizeOptions={[10, 25]}
                initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
                sx={dgSx}
              />
            </Box>
          </Paper>

          {/* Monitoring */}
          <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827' }}>
            <Box sx={{ p: 2, pb: 1, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  Live Monitoring
                </Typography>
                <Typography variant="caption" sx={{ color: 'var(--text-muted)' }}>
                  {isMonitoring
                    ? `Refreshing every 10s — last update ${lastMonitorRefresh ? new Date(lastMonitorRefresh).toLocaleTimeString() : '...'}`
                    : 'Monitoring stopped'}
                </Typography>
              </Box>
              <Box sx={{ display: 'flex', gap: 1 }}>
                <Button
                  size="small"
                  startIcon={<RefreshIcon />}
                  onClick={() => refreshMonitoring(scriptId)}
                  sx={{ textTransform: 'none', color: 'var(--text-secondary)' }}
                >
                  Refresh Now
                </Button>
                {isMonitoring ? (
                  <Button
                    size="small"
                    variant="outlined"
                    color="warning"
                    startIcon={<StopIcon />}
                    onClick={stopMonitoring}
                    sx={{ textTransform: 'none' }}
                  >
                    Stop Monitoring
                  </Button>
                ) : (
                  <Button
                    size="small"
                    variant="outlined"
                    startIcon={<RocketLaunchIcon />}
                    onClick={() => {
                      resetDeployState();
                    }}
                    sx={{ textTransform: 'none' }}
                  >
                    New Deployment
                  </Button>
                )}
              </Box>
            </Box>
            {isMonitoring && <LinearProgress sx={{ mx: 2 }} />}
            <Box sx={{ height: 350 }}>
              <DataGrid
                rows={monitoringStates}
                columns={monitorColumns}
                getRowId={(r) => `${r.deviceName}-${r.lastStateUpdateDateTime ?? ''}`}
                getRowHeight={() => 'auto'}
                pageSizeOptions={[25, 50]}
                initialState={{
                  pagination: { paginationModel: { pageSize: 25 } },
                  sorting: { sortModel: [{ field: 'lastStateUpdateDateTime', sort: 'desc' }] },
                }}
                showToolbar
                slotProps={{ toolbar: { showQuickFilter: true } }}
                sx={dgSx}
              />
            </Box>
          </Paper>
        </>
      )}
    </Box>
  );
}
