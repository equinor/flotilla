import { createContext, FC, useContext, useEffect, useState } from 'react'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { useRobotContext } from './RobotContext'
import { RemoteParticipant, RemoteTrack, RemoteTrackPublication, Room, RoomEvent } from 'livekit-client'
import { MediaConnectionType, MediaStreamConfig } from 'models/VideoStream'

type MediaStreamDictionaryType = {
    [robotId: string]: MediaStreamConfig & { streams: MediaStreamTrack[] }
}

interface IMediaStreamContext {
    mediaStreams: MediaStreamDictionaryType
}

interface Props {
    children: React.ReactNode
}

const defaultMediaStreamInterface = {
    mediaStreams: {},
}

export const MediaStreamContext = createContext<IMediaStreamContext>(defaultMediaStreamInterface)

export const MediaStreamProvider: FC<Props> = ({ children }) => {
    const [mediaStreams, setMediaStreams] = useState<MediaStreamDictionaryType>(
        defaultMediaStreamInterface.mediaStreams
    )
    const { registerEvent, connectionReady } = useSignalRContext()
    const { enabledRobots } = useRobotContext()

    const addTrackToConnection = (newTrack: MediaStreamTrack, robotId: string) => {
        setMediaStreams((oldStreams) => {
            if (!Object.keys(oldStreams).includes(robotId)) {
                return oldStreams
            } else {
                const newStreams = { ...oldStreams }
                return {
                    ...oldStreams,
                    [robotId]: { ...newStreams[robotId], streams: [...oldStreams[robotId].streams, newTrack] },
                }
            }
        })
    }

    const createLiveKitConnection = async (config: MediaStreamConfig) => {
        const room = new Room()
        room.on(RoomEvent.TrackSubscribed, handleTrackSubscribed)

        function handleTrackSubscribed(
            track: RemoteTrack,
            publication: RemoteTrackPublication,
            participant: RemoteParticipant
        ) {
            addTrackToConnection(track.mediaStreamTrack, config.robotId)
        }
        await room.connect(config.url, config.token)
    }

    const createMediaConnection = async (config: MediaStreamConfig) => {
        switch (config.mediaConnectionType) {
            case MediaConnectionType.LiveKit:
                return await createLiveKitConnection(config)
            default:
                console.error('Invalid media connection type received')
        }
        return undefined
    }

    // Register a signalR event handler that listens for new media stream connections
    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.mediaStreamConfigReceived, (username: string, message: string) => {
                const newMediaConfig: MediaStreamConfig = JSON.parse(message)
                setMediaStreams((oldStreams) => {
                    if (Object.keys(oldStreams).includes(newMediaConfig.robotId)) {
                        return oldStreams
                    } else {
                        createMediaConnection(newMediaConfig)
                        return {
                            ...oldStreams,
                            [newMediaConfig.robotId]: { ...newMediaConfig, streams: [] },
                        }
                    }
                })
            })
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [registerEvent, connectionReady, enabledRobots])

    return (
        <MediaStreamContext.Provider
            value={{
                mediaStreams,
            }}
        >
            {children}
        </MediaStreamContext.Provider>
    )
}

export const useMediaStreamContext = () => useContext(MediaStreamContext)
