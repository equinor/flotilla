import { BackButton } from 'utils/BackButton'
import { Header } from 'components/Header/Header'
import { StyledPage } from 'components/Styles/StyledComponents'
import { styled } from 'styled-components'
import { tokens } from '@equinor/eds-tokens'
import { ReactNode } from 'react'
import { StopRobotDialog } from '../FrontPage/MissionOverview/StopDialogs'

const StyledMissionHistoryPage = styled(StyledPage)`
    background-color: ${tokens.colors.ui.background__light.hex};
`
interface DefaultPageProps {
    children: ReactNode
    pageName: string
}

export const DefaultPage = ({ children, pageName }: DefaultPageProps) => {
    return (
        <>
            <Header page={pageName} />
            <StyledMissionHistoryPage>
                <BackButton />
                <StopRobotDialog />
                {children}
            </StyledMissionHistoryPage>
        </>
    )
}
