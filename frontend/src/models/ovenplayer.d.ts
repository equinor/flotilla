declare module 'ovenplayer' {
    interface Player {
        on(event: string, handler: (event: { newstate: string }) => void): void
        remove(): void
    }

    const OvenPlayer: {
        create(
            element: HTMLElement,
            options: {
                autoStart: boolean
                mute: boolean
                controls: boolean
                expandFullScreenUI: boolean
                sources: { type: string; file: string; label: string }[]
            }
        ): Player
    }
    export default OvenPlayer
}
