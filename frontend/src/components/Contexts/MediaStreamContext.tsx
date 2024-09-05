import { createContext, FC, useContext, useEffect, useState } from 'react'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { useRobotContext } from './RobotContext'
import { RemoteParticipant, RemoteTrack, RemoteTrackPublication, Room, RoomEvent } from 'livekit-client'

export enum MediaType {
    Video,
    Audio,
}

export enum MediaConnectionType {
    LiveKit,
}

type MediaStreamConfig = {
    url: string
    streamId: string
    authToken: string
    mediaType: MediaType
    robotId: string
    connectionType: MediaConnectionType
}

type MediaStreamConfigAndTracks = MediaStreamConfig & { streams: MediaStreamTrack[] }

type MediaStreamDictionaryType = {
    [robotId: string]: MediaStreamConfigAndTracks
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

    const addTracksToConnection = (newTracks: MediaStreamTrack[], robotId: string, streamId: string) => {
        setMediaStreams((oldStreams) => {
            if (!Object(oldStreams).keys.includes(robotId)) {
                return oldStreams
            } else {
                const newStreams = { ...oldStreams }
                // TODO: maybe have index be streamId or at least filter on it. Otherwise remove it and just display all video streams you get
                return {
                    ...oldStreams,
                    [robotId]: { ...newStreams[robotId], streams: newTracks },
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
            const videoTracks = track.mediaStream?.getVideoTracks()
            addTracksToConnection(videoTracks ?? [], config.robotId, config.streamId)
        }
        await room.connect(config.url, config.authToken)
    }

    const createMediaConnection = async (config: MediaStreamConfig) => {
        switch (config.connectionType) {
            case MediaConnectionType.LiveKit:
                return await createLiveKitConnection(config)
            default:
                console.error('Invalid media connection type received')
        }
        return undefined
    }

    // Register a signalR event handler that listens for new failed missions
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
