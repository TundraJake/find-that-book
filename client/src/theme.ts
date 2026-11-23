import { createTheme } from '@mui/material/styles'

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#1F3A5F', light: '#3D5A80', dark: '#14263F', contrastText: '#FFFFFF' },
    secondary: { main: '#C8763A', contrastText: '#FFFFFF' },
    background: { default: '#F6F4EF', paper: '#FFFFFF' },
    text: { primary: '#1B1F24', secondary: '#5B6470' },
    divider: '#E4E0D8',
  },
  shape: { borderRadius: 12 },
  typography: {
    fontFamily: '"Inter Variable", "Inter", system-ui, -apple-system, "Segoe UI", sans-serif',
    h1: { fontSize: '2.75rem', fontWeight: 700, letterSpacing: '-0.025em', lineHeight: 1.1 },
    h2: { fontSize: '1.25rem', fontWeight: 650, letterSpacing: '-0.01em' },
    h3: { fontSize: '1.125rem', fontWeight: 650, letterSpacing: '-0.01em', lineHeight: 1.3 },
    overline: { fontWeight: 600, letterSpacing: '0.1em' },
    button: { textTransform: 'none', fontWeight: 600 },
  },
  components: {
    MuiPaper: { defaultProps: { elevation: 0 } },
    MuiCard: {
      defaultProps: { elevation: 0 },
      styleOverrides: {
        root: ({ theme }) => ({
          border: `1px solid ${theme.palette.divider}`,
          transition: 'box-shadow 160ms ease, transform 160ms ease',
          '&:hover': { boxShadow: '0 8px 24px rgba(20, 38, 63, 0.08)', transform: 'translateY(-1px)' },
        }),
      },
    },
    MuiChip: { styleOverrides: { root: { fontWeight: 500 } } },
    MuiButton: { defaultProps: { disableElevation: true } },
  },
})
