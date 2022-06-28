import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'

const StyledTagCount = styled.div`
    display: flex;
`

export function MissionProgressDisplay() {
    return (
        <StyledTagCount>
            <Typography>Tag ?/?</Typography>
        </StyledTagCount>
    )
}
