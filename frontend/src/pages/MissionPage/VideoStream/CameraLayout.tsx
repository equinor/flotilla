import styled from 'styled-components'

export const CAMERA_GAP_REM = 1
export const DEFAULT_CAMERA_ASPECT_RATIO = 16 / 9

export const Cameras = styled.div`
    display: flex;
    align-items: flex-start;
    gap: ${CAMERA_GAP_REM}rem;
`

export const Camera = styled.div<{ $aspectRatio: number }>`
    flex: ${({ $aspectRatio }) => $aspectRatio} 1 0;
    min-width: 0;
`

export const CameraViewport = styled.div<{ $aspectRatio: number }>`
    position: relative;
    aspect-ratio: ${({ $aspectRatio }) => $aspectRatio};
    overflow: hidden;
    border-radius: 0.5rem;
    background: black;

    video {
        display: block;
        width: 100%;
        height: 100%;
        object-fit: contain;
    }
`
