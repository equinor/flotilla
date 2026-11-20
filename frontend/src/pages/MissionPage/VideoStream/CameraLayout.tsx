import styled from 'styled-components'

export const CAMERA_GAP_REM = 1

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
    width: 100%;
    aspect-ratio: ${({ $aspectRatio }) => $aspectRatio};
    overflow: hidden;
    border-radius: 0.5rem;
    background: #000;

    video {
        display: block;
        width: 100%;
        height: 100%;
        object-fit: contain;
    }
`
