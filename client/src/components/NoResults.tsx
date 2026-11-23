import SearchOffOutlinedIcon from '@mui/icons-material/SearchOffOutlined'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'

interface NoResultsProps {
  query: string
}

export function NoResults({ query }: NoResultsProps) {
  return (
    <Stack spacing={1.5} sx={{ alignItems: 'center', textAlign: 'center', py: 6, color: 'text.secondary' }}>
      <SearchOffOutlinedIcon sx={{ fontSize: 44, color: 'text.disabled' }} />
      <Typography variant="h3" component="p" color="text.primary">
        No matches for “{query}”
      </Typography>
      <Typography variant="body2" sx={{ maxWidth: 420 }}>
        Try adding an author’s last name or a distinctive word from the title.
      </Typography>
    </Stack>
  )
}
