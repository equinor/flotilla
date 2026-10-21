import { useEffect, useState } from 'react'
import { Button, Icon, Menu, Typography } from '@equinor/eds-core-react'
import { useBackendApi } from 'api/UseBackendApi'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { ApiError } from 'api/ApiError'

const Cameras = styled.div`
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: 1rem;
`

const Camera = styled.div`
    min-width: 0;
    max-width: 100%;
`

const Player = styled.div`
    width: fit-content;
    max-width: 100%;
    video {
        display: block;
        height: min(18rem, 50vw);
        width: auto;
        max-width: 100%;
    }
`

const PlayerToolbar = styled.div`
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
`

const RecordingPlayer = ({ camera }: { camera: MissionRecording['cameras'][string] }) => {
    const [isMenuOpen, setIsMenuOpen] = useState(false)
    const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null)

    return (
        <Player>
            <PlayerToolbar>
                <Typography variant="h5">{camera.cameraId}</Typography>
                {camera.downloadUrl && (
                    <>
                        <Button
                            variant="ghost_icon"
                            ref={setAnchorEl}
                            aria-label={`Video options for ${camera.cameraId}`}
                            aria-haspopup="menu"
                            aria-expanded={isMenuOpen}
                            onClick={() => setIsMenuOpen(!isMenuOpen)}
                        >
                            <Icon name={Icons.Menu} />
                        </Button>
                        <Menu
                            open={isMenuOpen}
                            anchorEl={anchorEl}
                            aria-label={`Video options for ${camera.cameraId}`}
                            onClose={() => setIsMenuOpen(false)}
                        >
                            <Menu.Item as="a" href={camera.downloadUrl} onClick={() => setIsMenuOpen(false)}>
                                Download video
                            </Menu.Item>
                        </Menu>
                    </>
                )}
            </PlayerToolbar>
            <video controls preload="metadata" src={camera.playbackUrl} />
        </Player>
    )
}

export interface MissionRecording {
    status: string
    engine?: string
    gaps: { at: string; reason: string }[]
    cameras: Record<
        string,
        {
            cameraId: string
            firstFrameAt?: string
            status: string
            error?: string
            playbackUrl?: string
            downloadUrl?: string
        }
    >
}

export const MissionRecordings = ({ missionId }: { missionId: string }) => {
    const backendApi = useBackendApi()
    const [recording, setRecording] = useState<MissionRecording>()
    const [error, setError] = useState<'unavailable' | 'forbidden'>()
    useEffect(() => {
        let disposed = false
        let timer: ReturnType<typeof setTimeout>
        const refresh = () =>
            backendApi
                .getMissionRecordings(missionId)
                .then((result) => {
                    if (!disposed) {
                        setRecording(result)
                        setError(undefined)
                        timer = setTimeout(() => void refresh(), result.status === 'Ready' ? 600000 : 2000)
                    }
                })
                .catch((requestError: unknown) => {
                    if (!disposed) {
                        const forbidden = requestError instanceof ApiError && requestError.statusCode === 403
                        setError(forbidden ? 'forbidden' : 'unavailable')
                        if (!forbidden) timer = setTimeout(() => void refresh(), 10000)
                    }
                })
        void refresh()
        return () => {
            disposed = true
            clearTimeout(timer)
        }
    }, [backendApi, missionId])

    if (error === 'forbidden' || (!recording && !error)) return null

    return (
        <section>
            <Typography variant="h2">Mission camera recordings</Typography>
            {(error || recording?.status !== 'Ready') && (
                <Typography role="status">
                    {error ? 'Recordings unavailable.' : (recording?.status ?? 'Loading recordings…')}
                </Typography>
            )}
            {!!recording?.gaps.length && (
                <Typography>Recording interruptions detected; some footage may be missing.</Typography>
            )}
            <Cameras>
                {Object.values(recording?.cameras ?? {}).map((camera) => (
                    <Camera key={camera.cameraId}>
                        {!camera.playbackUrl && <Typography variant="h5">{camera.cameraId}</Typography>}
                        {(camera.status !== 'Ready' || camera.error) && (
                            <Typography>
                                {camera.status}
                                {camera.error ? `: ${camera.error}` : ''}
                            </Typography>
                        )}
                        {camera.playbackUrl && <RecordingPlayer camera={camera} />}
                    </Camera>
                ))}
            </Cameras>
        </section>
    )
}
