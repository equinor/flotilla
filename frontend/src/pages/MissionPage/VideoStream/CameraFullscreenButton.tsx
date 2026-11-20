import { Button, Icon, Tooltip } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Icons } from 'utils/icons'

const FullscreenButton = styled(Button)`
    position: absolute;
    inset: auto ${tokens.spacings.comfortable.small} ${tokens.spacings.comfortable.small} auto;
    z-index: 1;
    opacity: 0.85;
`

export const CameraFullscreenButton = ({
    onClick,
    isFullscreen = false,
}: {
    onClick: () => void
    isFullscreen?: boolean
}) => {
    const label = isFullscreen ? 'Exit fullscreen' : 'Enter fullscreen'
    return (
        <Tooltip title={label} placement="top">
            <FullscreenButton
                variant="contained_icon"
                color="secondary"
                onClick={onClick}
                onDoubleClick={(event) => event.stopPropagation()}
                aria-label={label}
            >
                <Icon name={isFullscreen ? Icons.FullscreenExit : Icons.Fullscreen} size={24} />
            </FullscreenButton>
        </Tooltip>
    )
}
