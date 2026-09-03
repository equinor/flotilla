import { createContext, FC, useContext, useEffect, useMemo, useState } from 'react'
import { useBackendApi } from 'api/UseBackendApi'
import { createMediaStreamManager, MediaStreamState } from './MediaStreamManager'

interface IMediaStreamContext {
    mediaStreams: Record<string, MediaStreamState>
    acquireMediaStream: (robotId: string) => () => void
    retryMediaStream: (robotId: string) => void
}

const MediaStreamContext = createContext<IMediaStreamContext>({
    mediaStreams: {},
    acquireMediaStream: () => () => {},
    retryMediaStream: () => {},
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
            }}
        >
            {children}
        </MediaStreamContext.Provider>
    )
}

export const useMediaStreamContext = () => useContext(MediaStreamContext)
