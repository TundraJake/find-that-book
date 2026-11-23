import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import CloudOffOutlinedIcon from '@mui/icons-material/CloudOffOutlined'

interface CatalogUnavailableNoticeProps {
  onRetry: () => void
}

/** Shown when Open Library times out or fails (503, source "open_library"): usually brief, so offer a one-click retry. */
export function CatalogUnavailableNotice({ onRetry }: CatalogUnavailableNoticeProps) {
  return (
    <Alert
      severity="warning"
      variant="outlined"
      icon={<CloudOffOutlinedIcon />}
      sx={{ bgcolor: 'background.paper', alignItems: 'center' }}
      action={
        <Button size="small" variant="outlined" color="inherit" onClick={onRetry} sx={{ whiteSpace: 'nowrap' }}>
          Try again
        </Button>
      }
    >
      Open Library, the book catalog we search, isn&apos;t responding right now. This is usually brief. Please try again.
    </Alert>
  )
}
