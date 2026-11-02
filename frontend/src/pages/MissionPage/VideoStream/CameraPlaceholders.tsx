import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import {
    CAMERA_GAP_REM,
    CAMERA_LABEL_HEIGHT_REM,
    CAMERA_MEDIA_HEIGHT_REM,
    CAMERA_MIN_WIDTH_REM,
} from './cameraPositions'

const PlaceholderGrid = styled.div<{ $visible: boolean }>`
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(min(100%, ${CAMERA_MIN_WIDTH_REM}rem), 1fr));
    gap: ${CAMERA_GAP_REM}rem;
    padding: ${CAMERA_GAP_REM}rem 0;
    visibility: ${({ $visible }) => ($visible ? 'visible' : 'hidden')};
`

const PlaceholderLabel = styled.div`
    height: ${CAMERA_LABEL_HEIGHT_REM}rem;

    &::before {
        content: '';
        display: block;
        width: 40%;
        height: 0.75rem;
        background: ${tokens.colors.ui.background__medium.hex};
        border-radius: ${tokens.shape.corners.borderRadius};
    }
`

const PlaceholderFrame = styled.div`
    height: ${CAMERA_MEDIA_HEIGHT_REM}rem;
    background: ${tokens.colors.ui.background__light.hex};
    border: 1px solid ${tokens.colors.ui.background__medium.hex};
    border-radius: ${tokens.shape.corners.borderRadius};
    box-sizing: border-box;
`

export const CameraPlaceholders = ({ count, visible }: { count: number; visible: boolean }) => (
    <PlaceholderGrid $visible={visible} aria-hidden="true">
        {Array.from({ length: count }, (_, index) => (
            <div key={index}>
                <PlaceholderLabel />
                <PlaceholderFrame />
            </div>
        ))}
    </PlaceholderGrid>
)
