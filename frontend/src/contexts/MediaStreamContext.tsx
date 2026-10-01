import { createContext, FC, useContext, useEffect, useMemo, useState } from 'react'
import { useBackendApi } from 'api/UseBackendApi'
import { createMediaStreamManager, MediaStreamState } from './MediaStreamManager'

interface IMediaStreamContext extends Pick<
    ReturnType<typeof createMediaStreamManager>,
    'markOmeStreamPlaying' | 'markOmeStreamStalled' | 'reconnectOmeStream' | 'retryOmeStream'
> {
    mediaStreams: Record<string, MediaStreamState>
    acquireMediaStream: (robotId: string) => () => void
    retryMediaStream: (robotId: string) => void
}

const MediaStreamContext = createContext<IMediaStreamContext>({
    mediaStreams: {},
    acquireMediaStream: () => () => {},
    retryMediaStream: () => {},
    markOmeStreamPlaying: () => {},
    markOmeStreamStalled: () => {},
    reconnectOmeStream: () => {},
    retryOmeStream: () => {},
})

export const MediaStreamProvider: FC<{ children: React.ReactNode }> = ({ children }) => {
    const [mediaStreams, setMediaStreams] = useState<Record<string, MediaStreamState>>({})
    const backendApi = useBackendApi()
    const manager = useMemo(
        () =>
            createMediaStreamManager(
                (robotId) => backendApi.getRobotMediaConfig(robotId),
                (robotId, state) =>
                    setMediaStreams((previous) => {
                        const next = { ...previous }
                        if (state) next[robotId] = state
                        else delete next[robotId]
                        return next
                    })
            ),
        [backendApi]
    )

    useEffect(() => () => manager.dispose(), [manager])

    return (
        <MediaStreamContext.Provider
            value={{
                mediaStreams,
                acquireMediaStream: manager.acquire,
                retryMediaStream: manager.retry,
                markOmeStreamPlaying: manager.markOmeStreamPlaying,
                markOmeStreamStalled: manager.markOmeStreamStalled,
                reconnectOmeStream: manager.reconnectOmeStream,
                retryOmeStream: manager.retryOmeStream,
            }}
        >
            {children}
        </MediaStreamContext.Provider>
    )
}

export const useMediaStreamContext = () => useContext(MediaStreamContext)
