import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Skeleton from '@mui/material/Skeleton'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'

const placeholderCount = 3
/** Matches Gemini:TimeoutSeconds in appsettings.json; plain text on purpose. */
const waitNote = 'Asking Gemini and Open Library… answers can take up to 60 seconds.'

export function ResultsSkeleton() {
  return (
    <Stack spacing={2} aria-busy="true" aria-label="Loading results">
      <Typography variant="body2" color="text.secondary" role="status">
        {waitNote}
      </Typography>
      {Array.from({ length: placeholderCount }, (_, index) => (
        <Card key={index}>
          <CardContent>
            <Stack direction="row" spacing={2.5}>
              <Skeleton variant="rounded" width={96} height={144} />
              <Stack spacing={1} sx={{ flexGrow: 1 }}>
                <Skeleton width={110} height={24} />
                <Skeleton width="55%" height={30} />
                <Skeleton width="40%" />
                <Skeleton width="90%" />
                <Skeleton width="70%" />
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      ))}
    </Stack>
  )
}
