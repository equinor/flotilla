import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

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
    const { TranslateText } = useLanguageContext()
    return (
        <StyledPlaceholder>
            <Typography variant="h4" color="disabled">
                {' '}
                {TranslateText('No ongoing missions')}{' '}
            </Typography>
        </StyledPlaceholder>
    )
}

export function EmptyMissionQueuePlaceholder() {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledPlaceholder>
            <Typography variant="h4" color="disabled">
                {' '}
                {TranslateText('No missions in queue')}{' '}
            </Typography>
        </StyledPlaceholder>
    )
}
