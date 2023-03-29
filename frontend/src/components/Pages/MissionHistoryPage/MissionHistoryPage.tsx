import { MissionHistoryView } from './MissionHistoryView'
import { BackButton } from '../MissionPage/MissionHeader/BackButton'
import styled from 'styled-components'

const StyledMissionPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: start;
    flex-direction: column;
    gap: 1rem;
`

export type RefreshProps = {
    refreshInterval: number
}

export function MissionHistoryPage() {
    const refreshInterval = 1000

    return (
        <StyledMissionPage>
            <BackButton />
            <MissionHistoryView refreshInterval={refreshInterval} />
        </StyledMissionPage>
    )
}
