import Alert from '@mui/material/Alert'
import AlertTitle from '@mui/material/AlertTitle'
import Typography from '@mui/material/Typography'
import GppMaybeOutlinedIcon from '@mui/icons-material/GppMaybeOutlined'

/** Shown when the API refuses a query as unsafe (HTTP 422); worded neutrally so a false positive on a real title isn't accusatory. */
export function RejectedSearchNotice() {
  return (
    <Alert severity="warning" variant="outlined" icon={<GppMaybeOutlinedIcon />} sx={{ bgcolor: 'background.paper', py: 2 }}>
      <AlertTitle sx={{ fontWeight: 600 }}>We can&apos;t search for that</AlertTitle>
      <Typography variant="body2" sx={{ mb: 1 }}>
        This search was flagged by our content safety checks, so no results were produced.
      </Typography>
      <Typography variant="body2" color="text.secondary">
        Try describing the book by its title, author, characters, or plot details.
      </Typography>
    </Alert>
  )
}
