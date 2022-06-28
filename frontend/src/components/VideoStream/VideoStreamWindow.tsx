import { Card } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'

const VideoCard = styled(Card)`
    width: 400px;
    height: 400px;
`

export function VideoStreamWindow() {
    return (
        <VideoCard variant="default" style={{ boxShadow: tokens.elevation.sticky }}>
            Hello
        </VideoCard>
    )
}
