import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'

const StyledPlaceholder = styled.div`
    display: flex;
    box-sizing: border-box;
    flex-direction: row;
    align-items: flex-start;
    padding: 24px;
    gap: 8px;

    border: 1px solid #dcdcdc;
    border-radius: 4px;

    flex: none;
    order: 1;
    align-self: stretch;
    flex-grow: 1;
`

export function NoOngoingMissionsPlaceholder() {
    return (
        <StyledPlaceholder>
            <Typography variant="h4" color="disabled">
                {' '}
                No ongoing missions{' '}
            </Typography>
        </StyledPlaceholder>
    )
}

export function NoUpcomingMissionsPlaceholder() {
    return (
        <StyledPlaceholder>
            <Typography variant="h4" color="disabled">
                {' '}
                No pending missions in queue{' '}
            </Typography>
        </StyledPlaceholder>
    )
}
