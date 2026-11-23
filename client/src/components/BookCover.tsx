import MenuBookOutlinedIcon from '@mui/icons-material/MenuBookOutlined'
import Box from '@mui/material/Box'
import { useState } from 'react'

interface BookCoverProps {
  coverUrl: string | null
  title: string
}

export function BookCover({ coverUrl, title }: BookCoverProps) {
  const [hasFailed, setHasFailed] = useState(false)
  const showImage = coverUrl !== null && !hasFailed

  return (
    <Box
      sx={{
        width: { xs: 72, sm: 96 },
        aspectRatio: '2 / 3',
        flexShrink: 0,
        borderRadius: 1.5,
        overflow: 'hidden',
        bgcolor: 'background.default',
        border: 1,
        borderColor: 'divider',
        display: 'grid',
        placeItems: 'center',
        boxShadow: '0 4px 12px rgba(20, 38, 63, 0.12)',
      }}
    >
      {showImage ? (
        <Box component="img" src={coverUrl} alt={`Cover of ${title}`} loading="lazy" onError={() => setHasFailed(true)} sx={{ width: '100%', height: '100%', objectFit: 'cover' }} />
      ) : (
        <MenuBookOutlinedIcon sx={{ color: 'text.disabled', fontSize: 36 }} />
      )}
    </Box>
  )
}
