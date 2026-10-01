import { useEffect, useRef, useState } from 'react'
import { Button, Icon } from '@equinor/eds-core-react'
import { NamedMediaStream } from 'models/VideoStream'
import styled from 'styled-components'
import { Icons } from 'utils/icons'

const VideoFrame = styled.div`
    position: relative;
    width: 100%;
    aspect-ratio: 16 / 9;
    background: black;

    .ovenplayer {
        height: 100%;
        max-height: none;
    }

    .ovenplayer .op-ratio {
        display: none;
    }

    .ovenplayer video {
        object-fit: contain;
    }

    &:fullscreen {
        width: 100vw;
        height: 100vh;
        aspect-ratio: auto;
    }
`

const FullscreenButton = styled(Button)`
    position: absolute;
    right: 8px;
    bottom: 8px;
    z-index: 1;
    width: 40px;
    height: 40px;
    padding: 0;
    opacity: 0.85;
`

interface Props {
    stream: NamedMediaStream
    onPlaying: () => void
    onStalled: () => void
    onDisconnect: () => void
}

function requestOmeOffer(url: string, signal: AbortSignal): Promise<boolean> {
    return new Promise((resolve) => {
        if (signal.aborted) return resolve(false)
        const socket = new WebSocket(url)
        let finished = false
        const timeout = setTimeout(() => finish(false), 3000)
        signal.addEventListener('abort', abort, { once: true })
        function finish(ready: boolean) {
            if (finished) return
            finished = true
            clearTimeout(timeout)
            signal.removeEventListener('abort', abort)
            try {
                socket.close()
            } catch {
                // The connection may not have opened before the deadline.
            }
            resolve(ready)
        }
        function abort() {
            finish(false)
        }
        socket.onopen = () => socket.send(JSON.stringify({ command: 'request_offer' }))
        socket.onmessage = (event) => {
            try {
                finish(JSON.parse(event.data).command === 'offer')
            } catch {
                finish(false)
            }
        }
        socket.onerror = () => finish(false)
        socket.onclose = () => finish(false)
    })
}

export const OmeVideoCard = ({ stream, onPlaying, onStalled, onDisconnect }: Props) => {
    const element = useRef<HTMLDivElement>(null)
    const frame = useRef<HTMLDivElement>(null)
    const playingHandler = useRef(onPlaying)
    const stalledHandler = useRef(onStalled)
    const disconnectHandler = useRef(onDisconnect)
    const [isFullscreen, setIsFullscreen] = useState(false)

    useEffect(() => {
        const updateFullscreen = () => setIsFullscreen(document.fullscreenElement === frame.current)
        document.addEventListener('fullscreenchange', updateFullscreen)
        return () => document.removeEventListener('fullscreenchange', updateFullscreen)
    }, [])

    const toggleFullscreen = () => {
        if (document.fullscreenElement === frame.current) {
            void document.exitFullscreen()
        } else {
            void frame.current?.requestFullscreen()
        }
    }

    useEffect(() => {
        playingHandler.current = onPlaying
        stalledHandler.current = onStalled
        disconnectHandler.current = onDisconnect
    }, [onPlaying, onStalled, onDisconnect])

    useEffect(() => {
        let cancelled = false
        const abortController = new AbortController()
        let player: ReturnType<(typeof import('ovenplayer'))['default']['create']> | undefined
        let retryTimeout: ReturnType<typeof setTimeout> | undefined
        let waitForRetry: (() => void) | undefined

        const handleOmePlayerStateChange = ({ newstate }: { newstate: string }) => {
            if (cancelled) return
            if (newstate === 'playing') {
                playingHandler.current()
            } else if (['stalled', 'loading', 'paused', 'idle'].includes(newstate)) {
                stalledHandler.current()
            } else if (newstate === 'error') {
                disconnectHandler.current()
            }
        }

        const createOmePlayer = async () => {
            if (cancelled || !element.current) return
            const { default: OvenPlayer } = await import('ovenplayer')
            if (cancelled || !element.current) return
            player = OvenPlayer.create(element.current, {
                autoStart: true,
                mute: true,
                controls: false,
                expandFullScreenUI: false,
                sources: [{ type: 'webrtc', file: stream.url, label: stream.role }],
            })
            player.on('stateChanged', handleOmePlayerStateChange)
            player.on('error', () => {
                if (!cancelled) disconnectHandler.current()
            })
        }

        const connect = async () => {
            while (!cancelled) {
                if (await requestOmeOffer(stream.url, abortController.signal)) {
                    await createOmePlayer()
                    return
                }
                if (cancelled) return
                await new Promise<void>((resolve) => {
                    waitForRetry = resolve
                    retryTimeout = setTimeout(() => {
                        waitForRetry = undefined
                        resolve()
                    }, 1000)
                })
            }
        }

        void connect().catch(() => {
            if (!cancelled) disconnectHandler.current()
        })
        return () => {
            cancelled = true
            abortController.abort()
            clearTimeout(retryTimeout)
            waitForRetry?.()
            player?.remove()
        }
    }, [stream.url, stream.role])

    return (
        <VideoFrame ref={frame} onDoubleClick={toggleFullscreen}>
            <div ref={element} />
            <FullscreenButton
                color="secondary"
                onClick={toggleFullscreen}
                onDoubleClick={(event) => event.stopPropagation()}
                aria-label={isFullscreen ? 'Exit fullscreen' : 'Enter fullscreen'}
                title={isFullscreen ? 'Exit fullscreen' : 'Enter fullscreen'}
            >
                <Icon name={isFullscreen ? Icons.FullscreenExit : Icons.Fullscreen} size={24} />
            </FullscreenButton>
        </VideoFrame>
    )
}
