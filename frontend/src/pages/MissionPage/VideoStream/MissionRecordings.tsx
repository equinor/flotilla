import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button, Icon, Tooltip, Typography } from '@equinor/eds-core-react'
import { help_outline } from '@equinor/eds-icons'
import { useBackendApi } from 'api/UseBackendApi'
import styled from 'styled-components'
import { ApiError } from 'api/ApiError'
import { Mission } from 'models/Mission'
import { RecordingSeekRequest } from 'models/MissionRecording'
import { Icons } from 'utils/icons'
import { RecordingPlayer, RecordingPlayerControls } from './RecordingPlayer'
import { RecordingTimeline } from './RecordingTimeline'
import { getRecordingTimestamp } from './recordingTime'
import { useLanguageContext } from 'contexts/LanguageContext'

const RecordingSection = styled.section`
    display: flex;
    flex-direction: column;
    gap: 1rem;
    min-width: 0;
`

const RecordingHeader = styled.div`
    display: flex;
    align-items: center;
    gap: 0.5rem;
`

const Cameras = styled.div`
    display: flex;
    align-items: flex-start;
    gap: 1rem;
`

const Camera = styled.div<{ $aspectRatio: number }>`
    flex: ${({ $aspectRatio }) => $aspectRatio} 1 0;
    min-width: 0;
`

const PlaybackControls = styled.div`
    display: flex;
    justify-content: center;
    flex-wrap: wrap;
    gap: 0.75rem;

    > button {
        min-width: 7.5rem;
    }
`

interface MissionRecordingsProps {
    mission: Pick<Mission, 'id' | 'startTime' | 'endTime' | 'tasks'>
}

export const MissionRecordings = ({ mission }: MissionRecordingsProps) => {
    const backendApi = useBackendApi()
    const { TranslateText } = useLanguageContext()
    const [cameraAspectRatios, setCameraAspectRatios] = useState<Record<string, number>>({})
    const [seekRequest, setSeekRequest] = useState<RecordingSeekRequest>()
    const [playbackTimestamp, setPlaybackTimestamp] = useState<number>()
    const [playbackClock, setPlaybackClock] = useState<{ timestamp: number; startedAt: number }>()
    const [isVideoPlaying, setIsVideoPlaying] = useState(false)
    const players = useRef(new Map<string, RecordingPlayerControls>())
    const referenceCamera = useRef<string | undefined>(undefined)
    const missionId = mission.id
    const { data: recording, error } = useQuery({
        queryKey: ['missionRecordings', missionId],
        queryFn: () => backendApi.getMissionRecordings(missionId),
        retry: false,
        gcTime: 0,
        networkMode: 'always',
        refetchOnWindowFocus: false,
        refetchOnReconnect: false,
        refetchIntervalInBackground: true,
        refetchInterval: ({ state: { data, error } }) => {
            if (error instanceof ApiError && error.statusCode === 403) return false
            if (error) return 10000
            return data?.status === 'Ready' ? 600000 : 2000
        },
    })
    const missionStart = getRecordingTimestamp(mission.startTime)
    const missionEnd = getRecordingTimestamp(mission.endTime)
    const isPlaying = playbackClock !== undefined || isVideoPlaying

    const seekRecordings = (request: RecordingSeekRequest) => {
        referenceCamera.current = undefined
        setPlaybackTimestamp(request.timestamp)
        setSeekRequest(request)
        if (playbackClock) setPlaybackClock({ timestamp: request.timestamp, startedAt: performance.now() })
    }

    const playRecordings = () => {
        if (missionStart === undefined || missionEnd === undefined || missionEnd <= missionStart) {
            players.current.forEach((player) => player.play())
            return
        }
        const timestamp =
            playbackTimestamp === undefined || playbackTimestamp >= missionEnd ? missionStart : playbackTimestamp
        referenceCamera.current = undefined
        setPlaybackTimestamp(timestamp)
        setSeekRequest({ timestamp })
        setPlaybackClock({ timestamp, startedAt: performance.now() })
    }

    const pauseRecordings = () => {
        setPlaybackClock(undefined)
        setIsVideoPlaying(false)
        if (playbackClock && playbackTimestamp !== undefined) setSeekRequest({ timestamp: playbackTimestamp })
        players.current.forEach((player) => player.pause())
    }

    const updatePlaybackTimestamp = () => {
        if (playbackClock) return
        setIsVideoPlaying(Array.from(players.current.values()).some((player) => player.isPlaying()))
        let lastReferenceTimestamp: number | undefined
        for (const [cameraKey, player] of players.current) {
            const progress = player.getPlaybackProgress()
            if (!progress) continue
            if (progress.playing) {
                referenceCamera.current = cameraKey
                setPlaybackTimestamp(progress.timestamp)
                return
            }
            if (cameraKey === referenceCamera.current) lastReferenceTimestamp = progress.timestamp
        }
        if (lastReferenceTimestamp !== undefined) setPlaybackTimestamp(lastReferenceTimestamp)
    }

    useEffect(() => {
        if (!playbackClock || missionEnd === undefined) return

        const timer = setInterval(() => {
            const timestamp = Math.min(
                missionEnd,
                playbackClock.timestamp + performance.now() - playbackClock.startedAt
            )
            setPlaybackTimestamp(timestamp)
            if (timestamp >= missionEnd) {
                setPlaybackClock(undefined)
                setSeekRequest({ timestamp })
                players.current.forEach((player) => player.pause())
            }
        }, 100)
        return () => clearInterval(timer)
    }, [playbackClock, missionEnd])

    if ((error instanceof ApiError && error.statusCode === 403) || (!recording && !error)) return null

    const cameras = Object.values(recording?.cameras ?? {})
    const interruptionMessage = TranslateText('Recording interruptions detected; some footage may be missing.')

    return (
        <RecordingSection>
            <RecordingHeader>
                <Typography variant="h2">{TranslateText('Mission camera recordings')}</Typography>
                {!!recording?.gaps.length && (
                    <Tooltip placement="top" title={interruptionMessage}>
                        <Button variant="ghost_icon">
                            <Icon data={help_outline} />
                        </Button>
                    </Tooltip>
                )}
            </RecordingHeader>
            {(error || recording?.status !== 'Ready') && (
                <Typography role="status">
                    {error ? 'Recordings unavailable.' : (recording?.status ?? 'Loading recordings…')}
                </Typography>
            )}
            <Cameras>
                {cameras.map((camera) => (
                    <Camera key={camera.key} $aspectRatio={cameraAspectRatios[camera.key] ?? 16 / 9}>
                        {camera.status !== 'Ready' && <Typography variant="h5">{camera.cameraId}</Typography>}
                        {(camera.status !== 'Ready' || camera.error) && (
                            <Typography>
                                {camera.status}
                                {camera.error ? `: ${camera.error}` : ''}
                            </Typography>
                        )}
                        {camera.status === 'Ready' && (
                            <RecordingPlayer
                                ref={(player) => {
                                    if (player) players.current.set(camera.key, player)
                                    else players.current.delete(camera.key)
                                }}
                                missionId={missionId}
                                camera={camera}
                                seekRequest={seekRequest}
                                sharedPlaybackTimestamp={playbackClock ? playbackTimestamp : undefined}
                                onPlaybackProgress={updatePlaybackTimestamp}
                                onAspectRatioChange={(aspectRatio) =>
                                    setCameraAspectRatios((ratios) => ({ ...ratios, [camera.key]: aspectRatio }))
                                }
                            />
                        )}
                    </Camera>
                ))}
            </Cameras>
            {cameras.some((camera) => camera.status === 'Ready') && (
                <>
                    <RecordingTimeline
                        mission={mission}
                        seekRequest={seekRequest}
                        playbackTimestamp={playbackTimestamp}
                        onSeek={seekRecordings}
                    />
                    <PlaybackControls>
                        <Button onClick={isPlaying ? pauseRecordings : playRecordings}>
                            <Icon name={isPlaying ? Icons.PauseStandard : Icons.PlayStandard} />
                            {TranslateText(isPlaying ? 'Pause' : 'Play')}
                        </Button>
                    </PlaybackControls>
                </>
            )}
        </RecordingSection>
    )
}
