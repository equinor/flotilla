import { useId, useMemo } from 'react'
import { Slider, Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import { TaskStatus } from 'models/Task'
import { RecordingSeekRequest } from 'models/MissionRecording'
import { useLanguageContext } from 'contexts/LanguageContext'
import styled from 'styled-components'
import { formatRecordingTime, getRecordingTimestamp } from './recordingTime'
import { RecordingTimelineMarkers } from './RecordingTimelineMarkers'

const Timeline = styled(Typography)`
    font-variant-numeric: tabular-nums;
    min-width: 0;
    padding: 1rem;
`

const SeekArea = styled.div`
    overflow-x: auto;
    padding-block: 1.75rem 0.375rem;
`

const SeekTrack = styled.div`
    isolation: isolate;
    padding-inline: 2rem;
`

const SeekSlider = styled(Slider)`
    position: relative;
    z-index: 1;
    width: calc(100% + 12px);
    margin-inline: -6px;
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
    const markers = useMemo(
        () =>
            mission.tasks.flatMap((task) => {
                const timestamp = getRecordingTimestamp(task.startTime)
                if (
                    start === undefined ||
                    end === undefined ||
                    timestamp === undefined ||
                    timestamp < start ||
                    timestamp > end
                )
                    return []

                const offset = (timestamp - start) / 1000
                const label = task.tagId || task.description || `Task ${task.taskOrder}`
                const failed = task.status === TaskStatus.Failed
                return [{ id: task.id, order: task.taskOrder, timestamp, offset, label, failed }]
            }),
        [mission.tasks, start, end]
    )

    if (start === undefined || end === undefined || end <= start) {
        return <Typography>{TranslateText('Mission timing unavailable; use individual video controls.')}</Typography>
    }

    const duration = (end - start) / 1000
    const selectedTime = Math.max(0, Math.min(duration, ((playbackTimestamp ?? start) - start) / 1000))

    return (
        <Timeline as="div" variant="body_long">
            <Typography variant="body_short" role="group" title="Time from mission start / total duration">
                {formatRecordingTime(selectedTime)} / {formatRecordingTime(duration)}
            </Typography>
            <SeekArea>
                <SeekTrack>
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
                    <RecordingTimelineMarkers
                        markers={markers}
                        duration={duration}
                        seekRequest={seekRequest}
                        onSeek={onSeek}
                    />
                </SeekTrack>
            </SeekArea>
            {!markers.length && (
                <Typography>{TranslateText('No inspection task start times available on this timeline.')}</Typography>
            )}
        </Timeline>
    )
}
