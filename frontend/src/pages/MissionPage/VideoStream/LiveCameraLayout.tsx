import styled from 'styled-components'
import { tokens } from '@equinor/eds-tokens'
import { CAMERA_LABEL_HEIGHT_REM, CameraPosition } from './cameraPositions'

export const LiveCameraStage = styled.div`
    position: relative;
    min-width: 0;
    overflow: hidden;
`

export const LiveCameraRow = styled.div<{ $visible: boolean }>`
    position: absolute;
    inset: 0;
    visibility: ${({ $visible }) => ($visible ? 'visible' : 'hidden')};
`

export const LiveCamera = styled.div.attrs<{ $position?: CameraPosition }>(({ $position }) => ({
    style: {
        left: $position?.left ?? 0,
        top: $position?.top ?? 0,
        width: $position?.width ?? 0,
    },
}))`
    position: absolute;
    padding-top: ${CAMERA_LABEL_HEIGHT_REM}rem;

    > h5 {
        position: absolute;
        top: 0;
        height: ${CAMERA_LABEL_HEIGHT_REM}rem;
        max-width: 100%;
    }
`

export const CameraStatusOverlay = styled.div`
    position: absolute;
    inset: 0;
    z-index: 2;
    pointer-events: none;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: ${tokens.spacings.comfortable.medium_small};

    > button {
        pointer-events: auto;
    }
`

export const LiveCameraViewport = styled.div<{ $aspectRatio: number }>`
    position: relative;
    aspect-ratio: ${({ $aspectRatio }) => $aspectRatio};
    background: black;
`

export const LiveCameraStatus = styled.div`
    position: absolute;
    inset: 0;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: ${tokens.spacings.comfortable.medium_small};
    padding: ${tokens.spacings.comfortable.medium_small};
    background: rgb(0 0 0 / 60%);
    text-align: center;
`
