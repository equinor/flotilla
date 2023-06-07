import { BackButton } from '../MissionPage/MissionHeader/BackButton'
import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { AssetDecksDialog } from 'components/Pages/AssetDecks/AssetDecksDialog'

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

export function AssetDecksPage() {
    const refreshInterval = 1000

    return (
        <>
            <Header page={'assetdecks'} />
            <StyledMissionPage>
                <BackButton />
                <AssetDecksDialog refreshInterval={refreshInterval} />
            </StyledMissionPage>
        </>
    )
}
