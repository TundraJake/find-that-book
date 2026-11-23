import { useCallback, useEffect, useRef, useState, type KeyboardEvent, type PointerEvent } from 'react'

interface ResizablePanelOptions {
  defaultWidth: number
  minimumWidth: number
  maximumWidth: number
  /** Space always left for the content beside the panel. */
  minimumSiblingWidth: number
  keyboardStep: number
  storageKey: string
}

function readStoredWidth(storageKey: string): number | null {
  try {
    const stored = window.localStorage.getItem(storageKey)
    const parsed = stored === null ? Number.NaN : Number(stored)
    return Number.isFinite(parsed) ? parsed : null
  } catch {
    return null
  }
}

function writeStoredWidth(storageKey: string, width: number): void {
  try {
    window.localStorage.setItem(storageKey, String(Math.round(width)))
  } catch {
    // Storage can be unavailable (private mode, sandboxed frames); resizing still works for this session.
  }
}

/**
 * Width state for a panel docked to the right edge of the viewport, resized by dragging its left edge.
 * Returns props for the drag handle: pointer drag, arrow-key nudging, double-click to reset.
 */
export function useResizablePanelWidth(options: ResizablePanelOptions) {
  const { defaultWidth, minimumWidth, maximumWidth, minimumSiblingWidth, keyboardStep, storageKey } = options

  const clampWidth = useCallback(
    (width: number) => {
      const viewportLimit = window.innerWidth - minimumSiblingWidth
      const upperBound = Math.max(minimumWidth, Math.min(maximumWidth, viewportLimit))
      return Math.min(Math.max(width, minimumWidth), upperBound)
    },
    [minimumWidth, maximumWidth, minimumSiblingWidth],
  )

  const [width, setWidth] = useState(() => clampWidth(readStoredWidth(storageKey) ?? defaultWidth))
  const [isDragging, setIsDragging] = useState(false)
  const widthRef = useRef(width)

  useEffect(() => {
    widthRef.current = width
  }, [width])

  const applyWidth = useCallback(
    (nextWidth: number) => {
      const clamped = clampWidth(nextWidth)
      setWidth(clamped)
      writeStoredWidth(storageKey, clamped)
    },
    [clampWidth, storageKey],
  )

  useEffect(() => {
    function handleWindowResize() {
      setWidth((current) => clampWidth(current))
    }
    window.addEventListener('resize', handleWindowResize)
    return () => window.removeEventListener('resize', handleWindowResize)
  }, [clampWidth])

  useEffect(() => {
    if (!isDragging) return
    const previousCursor = document.body.style.cursor
    const previousUserSelect = document.body.style.userSelect
    document.body.style.cursor = 'col-resize'
    document.body.style.userSelect = 'none'
    return () => {
      document.body.style.cursor = previousCursor
      document.body.style.userSelect = previousUserSelect
    }
  }, [isDragging])

  function handlePointerDown(event: PointerEvent<HTMLElement>) {
    event.preventDefault()
    event.currentTarget.setPointerCapture(event.pointerId)
    setIsDragging(true)
  }

  function handlePointerMove(event: PointerEvent<HTMLElement>) {
    if (!isDragging) return
    const nextWidth = clampWidth(window.innerWidth - event.clientX)
    setWidth(nextWidth)
  }

  function handlePointerUp(event: PointerEvent<HTMLElement>) {
    if (!isDragging) return
    event.currentTarget.releasePointerCapture(event.pointerId)
    setIsDragging(false)
    writeStoredWidth(storageKey, widthRef.current)
  }

  function handleKeyDown(event: KeyboardEvent<HTMLElement>) {
    const keyActions: Record<string, () => number> = {
      ArrowLeft: () => widthRef.current + keyboardStep,
      ArrowRight: () => widthRef.current - keyboardStep,
      Home: () => maximumWidth,
      End: () => minimumWidth,
    }
    const action = keyActions[event.key]
    if (action === undefined) return
    event.preventDefault()
    applyWidth(action())
  }

  return {
    width,
    isDragging,
    handleProps: {
      role: 'separator',
      tabIndex: 0,
      'aria-orientation': 'vertical' as const,
      'aria-valuenow': Math.round(width),
      'aria-valuemin': minimumWidth,
      'aria-valuemax': maximumWidth,
      onPointerDown: handlePointerDown,
      onPointerMove: handlePointerMove,
      onPointerUp: handlePointerUp,
      onPointerCancel: handlePointerUp,
      onKeyDown: handleKeyDown,
      onDoubleClick: () => applyWidth(defaultWidth),
    },
  }
}
