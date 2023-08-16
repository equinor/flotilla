import { BackButton } from '../MissionPage/MissionHeader/BackButton'
import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { AreasDialog } from 'components/Pages/InspectionPage/InspectionDialog'

const StyledMissionPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: start;
    flex-direction: column;
    gap: 1rem;
    margin: 2rem;
`

const StyledBarContent = styled.div`
    display: grid;
    grid-template-columns: minmax(50px, 265px) auto;
    align-items: end;
    gap: 0px 3rem;
`

export type RefreshProps = {
    refreshInterval: number
}

export function InspectionPage() {
    const refreshInterval = 1000

    return (
        <>
            <Header page={'inspections'} />
            <StyledMissionPage>
                <BackButton />
                <AreasDialog refreshInterval={refreshInterval} />
            </StyledMissionPage>
        </>
    )
}
