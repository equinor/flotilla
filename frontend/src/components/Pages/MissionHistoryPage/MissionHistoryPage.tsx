import { MissionHistoryView } from './MissionHistoryView'
import { BackButton } from 'utils/BackButton'
import { Header } from 'components/Header/Header'
import { StyledPage } from 'components/Styles/StyledComponents'
import { styled } from 'styled-components'
import { tokens } from '@equinor/eds-tokens'

export type RefreshProps = {
    refreshInterval: number
}

const StyledMissionHistoryPage = styled(StyledPage)`
    background-color: ${tokens.colors.ui.background__light.hex};
`
export const MissionHistoryPage = () => {
    const refreshInterval = 1000

    return (
        <>
            <Header page={'history'} />
            <StyledMissionHistoryPage>
                <BackButton />
                <MissionHistoryView refreshInterval={refreshInterval} />
            </StyledMissionHistoryPage>
        </>
    )
}
