import { CSSProperties, useLayoutEffect, useRef, useState } from 'react'
import { Button } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { RecordingSeekRequest } from 'models/MissionRecording'
import { formatRecordingTime } from './recordingTime'
import { getRecordingMarkerRows } from './recordingMarkerLayout'

const Markers = styled.div`
    position: relative;
    isolation: isolate;
    margin-top: -0.375rem;
`

const MarkerPosition = styled.div`
    position: absolute;

    &::before {
        content: '';
        position: absolute;
        z-index: -1;
        bottom: 100%;
        left: 0;
        height: var(--connector-height);
        border-left: 1px solid ${tokens.colors.ui.background__medium.hex};
        pointer-events: none;
    }
`

const Marker = styled(Button)`
    transform: translateX(-50%);
    min-width: 1.875rem;
    height: 1.625rem;
    padding: 0 0.25rem;
    font-size: 0.75rem;

    &[aria-pressed='false']:not(:hover):not(:active) {
        background-color: ${tokens.colors.ui.background__default.hex};
    }
`

interface RecordingTimelineMarker {
    id: string
    order: number
    timestamp: number
    offset: number
    label: string
    failed: boolean
}

interface RecordingTimelineMarkersProps {
    markers: RecordingTimelineMarker[]
    duration: number
    seekRequest: RecordingSeekRequest | undefined
    onSeek: (request: RecordingSeekRequest) => void
}

export const RecordingTimelineMarkers = ({ markers, duration, seekRequest, onSeek }: RecordingTimelineMarkersProps) => {
    const container = useRef<HTMLDivElement>(null)
    const [rows, setRows] = useState<number[]>([])

    useLayoutEffect(() => {
        const element = container.current
        if (!element) return

        const buttons = Array.from(element.querySelectorAll('button'))
        const updateRows = () => {
            const width = element.getBoundingClientRect().width
            const bounds = buttons.map((button, index) => ({
                left: (markers[index].offset / duration) * width,
                width: button.getBoundingClientRect().width,
            }))
            const nextRows = getRecordingMarkerRows(bounds)
            setRows((current) =>
                current.length === nextRows.length && current.every((row, index) => row === nextRows[index])
                    ? current
                    : nextRows
            )
        }

        updateRows()
        const observer = new ResizeObserver(updateRows)
        observer.observe(element)
        buttons.forEach((button) => observer.observe(button))
        return () => observer.disconnect()
    }, [markers, duration])

    return (
        <Markers ref={container} style={{ height: `${(Math.max(0, ...rows) + 1) * 2 + 0.25}rem` }}>
            {markers.map((marker, index) => {
                const row = rows[index] ?? 0
                const description = `${marker.label} at ${formatRecordingTime(marker.offset)} (approximate task start)`
                return (
                    <MarkerPosition
                        key={marker.id}
                        style={
                            {
                                left: `${(marker.offset / duration) * 100}%`,
                                top: `${row * 2 + 0.5}rem`,
                                '--connector-height': `${row * 2 + 1}rem`,
                            } as CSSProperties
                        }
                    >
                        <Marker
                            variant={seekRequest?.timestamp === marker.timestamp ? 'contained' : 'ghost'}
                            color={marker.failed ? 'danger' : 'primary'}
                            aria-pressed={seekRequest?.timestamp === marker.timestamp}
                            aria-label={`${marker.order}: ${description}`}
                            type="button"
                            title={description}
                            onClick={() => onSeek({ timestamp: marker.timestamp })}
                        >
                            {marker.order}
                        </Marker>
                    </MarkerPosition>
                )
            })}
        </Markers>
    )
}
