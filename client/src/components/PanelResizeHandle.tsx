import Box from '@mui/material/Box'
import Tooltip from '@mui/material/Tooltip'
import type { ComponentProps } from 'react'

const hitAreaPixels = 10

type PanelResizeHandleProps = Omit<ComponentProps<typeof Box>, 'sx'> & {
  isDragging: boolean
}

/** Vertical drag handle sitting on the left edge of a right-docked panel. */
export function PanelResizeHandle({ isDragging, ...handleProps }: PanelResizeHandleProps) {
  return (
    <Tooltip title="Drag to resize · double-click to reset" placement="left" disableInteractive enterDelay={600}>
      <Box
        aria-label="Resize JSON inspector"
        {...handleProps}
        sx={{
          position: 'absolute',
          top: 0,
          bottom: 0,
          left: -hitAreaPixels / 2,
          width: hitAreaPixels,
          zIndex: 2,
          cursor: 'col-resize',
          touchAction: 'none',
          outline: 'none',
          display: { xs: 'none', lg: 'block' },
          '&::after': {
            content: '""',
            position: 'absolute',
            top: 0,
            bottom: 0,
            left: '50%',
            width: 3,
            transform: 'translateX(-50%)',
            bgcolor: isDragging ? 'primary.main' : 'transparent',
            transition: 'background-color 120ms ease',
          },
          '&:hover::after, &:focus-visible::after': { bgcolor: 'primary.light' },
        }}
      />
    </Tooltip>
  )
}
