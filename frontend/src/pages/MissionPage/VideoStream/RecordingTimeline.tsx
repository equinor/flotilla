import { useId } from 'react'
import { Button, Slider, Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import { TaskStatus } from 'models/Task'
import { RecordingSeekRequest } from 'models/MissionRecording'
import { useLanguageContext } from 'contexts/LanguageContext'
import styled from 'styled-components'
import { formatRecordingTime, getRecordingTimestamp } from './recordingTime'

const Timeline = styled(Typography)`
    font-variant-numeric: tabular-nums;
    min-width: 0;
    padding: 1rem;
`

const SeekArea = styled.div`
    overflow-x: auto;
    padding-block: 1.75rem 0.375rem;
`

const SeekTrack = styled.div<{ $minimumWidth: number }>`
    isolation: isolate;
    min-width: ${({ $minimumWidth }) => $minimumWidth}px;
    padding-inline: 1rem;
`

const SeekSlider = styled(Slider)`
    position: relative;
    z-index: 1;
    width: calc(100% + 12px);
    margin-inline: -6px;
`

const Markers = styled.div`
    position: relative;
    z-index: 0;
    height: 2.25rem;
    margin-top: -0.375rem;
`

const Marker = styled(Button)`
    position: absolute;
    top: 0.5rem;
    transform: translateX(-50%);
    min-width: 1.875rem;
    height: 1.625rem;
    padding: 0 0.25rem;
    font-size: 0.75rem;

    &::before {
        content: '';
        position: absolute;
        bottom: 100%;
        left: 50%;
        transform: translateX(-50%);
        height: 1rem;
        border-left: inherit;
        background: inherit;
        pointer-events: none;
    }
`

interface RecordingTimelineProps {
    mission: Pick<Mission, 'startTime' | 'endTime' | 'tasks'>
    seekRequest: RecordingSeekRequest | undefined
    playbackTimestamp: number | undefined
    onSeek: (request: RecordingSeekRequest) => void
}

export const RecordingTimeline = ({ mission, seekRequest, playbackTimestamp, onSeek }: RecordingTimelineProps) => {
    const { TranslateText } = useLanguageContext()
    const timelineLabelId = useId()
    const start = getRecordingTimestamp(mission.startTime)
    const end = getRecordingTimestamp(mission.endTime)

    if (start === undefined || end === undefined || end <= start) {
        return <Typography>{TranslateText('Mission timing unavailable; use individual video controls.')}</Typography>
    }

    const duration = (end - start) / 1000
    const selectedTime = Math.max(0, Math.min(duration, ((playbackTimestamp ?? start) - start) / 1000))
    const markers = mission.tasks.flatMap((task) => {
        const timestamp = getRecordingTimestamp(task.startTime)
        if (timestamp === undefined || timestamp < start || timestamp > end) return []

        const offset = (timestamp - start) / 1000
        const label = task.tagId || task.description || `Task ${task.taskOrder}`
        const failed = task.status === TaskStatus.Failed
        return [{ id: task.id, order: task.taskOrder, timestamp, offset, label, failed }]
    })

    return (
        <Timeline as="div" variant="body_long">
            <Typography variant="body_short" role="group" title="Time from mission start / total duration">
                {formatRecordingTime(selectedTime)} / {formatRecordingTime(duration)}
            </Typography>
            <SeekArea>
                <SeekTrack $minimumWidth={markers.length * 40}>
                    <span id={timelineLabelId} hidden>
                        Mission timeline
                    </span>
                    <SeekSlider
                        aria-labelledby={timelineLabelId}
                        min={0}
                        max={duration}
                        step={0.1}
                        value={selectedTime}
                        minMaxDots={false}
                        minMaxValues={false}
                        outputFunction={formatRecordingTime}
                        onChange={(_, [seconds]) => onSeek({ timestamp: start + seconds * 1000 })}
                    />
                    <Markers>
                        {markers.map((marker) => {
                            const description = `${marker.label} at ${formatRecordingTime(marker.offset)} (approximate task start)`
                            return (
                                <Marker
                                    key={marker.id}
                                    variant={seekRequest?.timestamp === marker.timestamp ? 'contained' : 'outlined'}
                                    color={marker.failed ? 'danger' : 'primary'}
                                    aria-pressed={seekRequest?.timestamp === marker.timestamp}
                                    type="button"
                                    style={{ left: `${(marker.offset / duration) * 100}%` }}
                                    title={description}
                                    onClick={() => onSeek({ timestamp: marker.timestamp })}
                                >
                                    {marker.order}
                                </Marker>
                            )
                        })}
                    </Markers>
                </SeekTrack>
            </SeekArea>
            {!markers.length && (
                <Typography>{TranslateText('No inspection task start times available on this timeline.')}</Typography>
            )}
        </Timeline>
    )
}
