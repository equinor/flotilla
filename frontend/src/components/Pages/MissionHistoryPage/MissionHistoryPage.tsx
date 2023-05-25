import { MissionHistoryView } from './MissionHistoryView'
import { BackButton } from '../MissionPage/MissionHeader/BackButton'
import { Header } from 'components/Header/Header'
import styled from 'styled-components'

const StyledMissionPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: start;
    flex-direction: column;
    gap: 1rem;
    margin: 2rem;
`

export type RefreshProps = {
    refreshInterval: number
}

export function MissionHistoryPage() {
    const refreshInterval = 1000

    return (
        <>
            <Header page={'history'} />
            <StyledMissionPage>
                <BackButton />
                <MissionHistoryView refreshInterval={refreshInterval} />
            </StyledMissionPage>
        </>
    )
}
