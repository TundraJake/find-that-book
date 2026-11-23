import LocalLibraryOutlinedIcon from '@mui/icons-material/LocalLibraryOutlined'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'

export function EmptyState() {
  return (
    <Stack spacing={1.5} sx={{ alignItems: 'center', textAlign: 'center', py: 8, color: 'text.secondary' }}>
      <LocalLibraryOutlinedIcon sx={{ fontSize: 48, color: 'text.disabled' }} />
      <Typography variant="h3" component="p" color="text.primary">
        Half-remembered a book?
      </Typography>
      <Typography variant="body2" sx={{ maxWidth: 440 }}>
        Type whatever you’ve got: a partial title, an author’s last name, a character, even a year. We’ll work out which book you mean.
      </Typography>
    </Stack>
  )
}
