import AutoStoriesOutlinedIcon from '@mui/icons-material/AutoStoriesOutlined'
import GitHubIcon from '@mui/icons-material/GitHub'
import AppBar from '@mui/material/AppBar'
import Box from '@mui/material/Box'
import Container from '@mui/material/Container'
import IconButton from '@mui/material/IconButton'
import Toolbar from '@mui/material/Toolbar'
import Tooltip from '@mui/material/Tooltip'
import Typography from '@mui/material/Typography'
import type { AiUsage } from '../api/usageClient.ts'
import { AiUsageIndicator } from './AiUsageIndicator.tsx'

interface AppHeaderProps {
  usage: AiUsage | null
}

export function AppHeader({ usage }: AppHeaderProps) {
  return (
    <AppBar
      position="sticky"
      color="inherit"
      elevation={0}
      sx={{ borderBottom: 1, borderColor: 'divider', bgcolor: 'rgba(255,255,255,0.85)', backdropFilter: 'blur(8px)' }}
    >
      <Container maxWidth={false}>
        <Toolbar disableGutters sx={{ gap: 1.5 }}>
          <Box sx={{ display: 'grid', placeItems: 'center', width: 36, height: 36, borderRadius: 2, bgcolor: 'primary.main', color: 'primary.contrastText' }}>
            <AutoStoriesOutlinedIcon fontSize="small" />
          </Box>
          <Typography variant="h2" component="span" sx={{ flexGrow: 1 }}>
            Find That Book
          </Typography>
          <AiUsageIndicator usage={usage} />
          <Tooltip title="Source code">
            <IconButton aria-label="Source code" color="primary" size="small">
              <GitHubIcon />
            </IconButton>
          </Tooltip>
        </Toolbar>
      </Container>
    </AppBar>
  )
}
