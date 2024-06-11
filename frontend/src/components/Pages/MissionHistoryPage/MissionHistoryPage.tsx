import { MissionHistoryView } from './MissionHistoryView'
import { BackButton } from 'utils/BackButton'
import { Header } from 'components/Header/Header'
import { StyledPage } from 'components/Styles/StyledComponents'

export type RefreshProps = {
    refreshInterval: number
}

export const MissionHistoryPage = () => {
    const refreshInterval = 1000

    return (
        <>
            <Header page={'history'} />
            <StyledPage>
                <BackButton />
                <MissionHistoryView refreshInterval={refreshInterval} />
            </StyledPage>
        </>
    )
}
