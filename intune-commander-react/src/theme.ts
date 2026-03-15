import { createTheme } from '@mui/material/styles';

export const darkTheme = createTheme({
  palette: {
    mode: 'dark',
    background: {
      default: '#0b1020',
      paper: '#101828',
    },
    primary: {
      main: '#a78bfa',
    },
    text: {
      primary: '#f8fafc',
      secondary: '#94a3b8',
    },
    divider: '#243244',
  },
  typography: {
    fontFamily: "Inter, ui-sans-serif, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
    fontSize: 13,
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        // Don't let MUI reset override our existing tokens.css
        body: { background: 'transparent' },
      },
    },
  },
});
