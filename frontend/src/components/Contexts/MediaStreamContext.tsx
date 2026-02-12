import { createContext, FC, useContext, useEffect, useState } from 'react'
import { ConnectionState, Room, RoomEvent } from 'livekit-client'
import { MediaConnectionType, MediaStreamConfig } from 'models/VideoStream'
import { useBackendApi } from 'api/UseBackendApi'

type MediaStreamDictionaryType = {
    [robotId: string]: { isLoading: boolean } & MediaStreamConfig & { streams: MediaStreamTrack[] }
}

type MediaStreamConfigDictionaryType = {
    [robotId: string]: MediaStreamConfig
}

interface IMediaStreamContext {
    mediaStreams: MediaStreamDictionaryType
    addMediaStreamConfigIfItDoesNotExist: (robotId: string) => void
}

interface Props {
    children: React.ReactNode
}

const defaultMediaStreamInterface = {
    mediaStreams: {},
    addMediaStreamConfigIfItDoesNotExist: () => {},
}

const MediaStreamContext = createContext<IMediaStreamContext>(defaultMediaStreamInterface)

export const MediaStreamProvider: FC<Props> = ({ children }) => {
    const [mediaStreams, setMediaStreams] = useState<MediaStreamDictionaryType>(
        defaultMediaStreamInterface.mediaStreams
    )
    const [cachedConfigs] = useState<MediaStreamConfigDictionaryType>(
        JSON.parse(window.localStorage.getItem('mediaConfigs') ?? '{}')
    )
    const backendApi = useBackendApi()

    useEffect(() => {
        // Here we maintain the localstorage with the connection details
        const updatedConfigs: MediaStreamConfigDictionaryType = {}
        Object.keys(mediaStreams).forEach((robotId) => {
            const conf = mediaStreams[robotId]

            if (conf.streams.length === 0 && !conf.isLoading) refreshRobotMediaConfig(robotId)
            updatedConfigs[robotId] = {
                url: conf.url,
                token: conf.token,
                mediaConnectionType: conf.mediaConnectionType,
                robotId: conf.robotId,
            }
        })
        window.localStorage.setItem('mediaConfigs', JSON.stringify(updatedConfigs))
    }, [mediaStreams])

    const addTrackToConnection = (newTrack: MediaStreamTrack, robotId: string) => {
        setMediaStreams((oldStreams) => {
            if (
                !Object.keys(oldStreams).includes(robotId) ||
                oldStreams[robotId].streams.find((s) => s.id === newTrack.id)
            ) {
                return oldStreams
            } else {
                return {
                    ...oldStreams,
                    [robotId]: {
                        ...oldStreams[robotId],
                        streams: [...oldStreams[robotId].streams, newTrack],
                        isLoading: false,
                    },
                }
            }
        })
    }

    const createLiveKitConnection = async (config: MediaStreamConfig, cachedConfig: boolean = false) => {
        const room = new Room()

        window.addEventListener('unload', async () => room.disconnect())

        room.on(RoomEvent.TrackSubscribed, (track) => addTrackToConnection(track.mediaStreamTrack, config.robotId))
        room.on(RoomEvent.TrackUnpublished, (e) => {
            setMediaStreams((oldStreams) => {
                const streamsCopy = { ...oldStreams }
                if (!Object.keys(streamsCopy).includes(config.robotId) || streamsCopy[config.robotId].isLoading)
                    return streamsCopy

                const streamList = streamsCopy[config.robotId].streams
                const streamIndex = streamList.findIndex((s) => s.id === e.trackSid)

                if (streamIndex < 0) return streamsCopy

                streamList.splice(streamIndex, 1)
                streamsCopy[config.robotId].streams = streamList

                if (streamList.length === 0) room.disconnect()

                return streamsCopy
            })
        })

        if (room.state === ConnectionState.Disconnected) {
            room.connect(config.url, config.token)
                .then(() => console.log('LiveKit room status: ', JSON.stringify(room.state)))
                .catch((error) => {
                    if (cachedConfig) refreshRobotMediaConfig(config.robotId)
                    else console.error('Failed to connect to LiveKit room: ', error)
                })
        }
    }

    const createMediaConnection = async (config: MediaStreamConfig, cachedConfig: boolean = false) => {
        switch (config.mediaConnectionType) {
            case MediaConnectionType.LiveKit:
                return await createLiveKitConnection(config, cachedConfig)
            default:
                console.error('Invalid media connection type received')
        }
        return undefined
    }

    const addConfigToMediaStreams = (conf: MediaStreamConfig, cachedConfig: boolean = false) => {
        setMediaStreams((oldStreams) => {
            createMediaConnection(conf, cachedConfig)
            return {
                ...oldStreams,
                [conf.robotId]: { ...conf, streams: [], isLoading: true },
            }
        })
    }

    const addMediaStreamConfigIfItDoesNotExist = (robotId: string) => {
        if (Object.keys(mediaStreams).includes(robotId)) {
            const currentStream = mediaStreams[robotId]
            if (currentStream.isLoading || currentStream.streams.find((stream) => stream.enabled)) return
        } else if (Object.keys(cachedConfigs).includes(robotId)) {
            const config = cachedConfigs[robotId]
            addConfigToMediaStreams(config, true)
            return
        }

        refreshRobotMediaConfig(robotId)
    }

    const refreshRobotMediaConfig = (robotId: string) => {
        backendApi
            .getRobotMediaConfig(robotId)
            .then((conf: MediaStreamConfig | null | undefined) => {
                if (conf) addConfigToMediaStreams(conf)
            })
            .catch(() => console.log(`No media config found for robot with ID ${robotId}`))
    }

    return (
        <MediaStreamContext.Provider
            value={{
                mediaStreams,
                addMediaStreamConfigIfItDoesNotExist,
            }}
        >
            {children}
        </MediaStreamContext.Provider>
    )
}

export const useMediaStreamContext = () => useContext(MediaStreamContext)
