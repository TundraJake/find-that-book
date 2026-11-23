import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Link from '@mui/material/Link'
import OpenInNewIcon from '@mui/icons-material/OpenInNew'
import PersonSearchOutlinedIcon from '@mui/icons-material/PersonSearchOutlined'
import type { MatchedAuthor } from '../types.ts'

interface AuthorLinkCardProps {
  author: MatchedAuthor
  /** The title Gemini read from the query, if any; changes the wording. */
  queriedTitle: string | null
}

/** Shown when the author was found but no result is an exact title + primary author match: a shortcut to all their books. */
export function AuthorLinkCard({ author, queriedTitle }: AuthorLinkCardProps) {
  const message = queriedTitle
    ? <>No exact match for “{queriedTitle}”, but we found the author, <strong>{author.name}</strong>.</>
    : <>Looking for something by <strong>{author.name}</strong>?</>

  return (
    <Alert
      severity="info"
      variant="outlined"
      icon={<PersonSearchOutlinedIcon />}
      sx={{ bgcolor: 'background.paper', alignItems: 'center' }}
      action={
        <Button
          component={Link}
          href={author.open_library_url}
          target="_blank"
          rel="noopener noreferrer"
          size="small"
          variant="outlined"
          endIcon={<OpenInNewIcon fontSize="small" />}
          sx={{ whiteSpace: 'nowrap' }}
        >
          All their books
        </Button>
      }
    >
      {message} See everything they wrote on Open Library.
    </Alert>
  )
}
