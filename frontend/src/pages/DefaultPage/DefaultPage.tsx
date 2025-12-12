import { BackButton } from 'utils/BackButton'
import { Header } from 'components/Header/Header'
import { StyledPage } from 'components/Styles/StyledComponents'
import { ReactNode } from 'react'
import { StopRobotDialog } from '../FrontPage/MissionOverview/StopDialogs'

interface DefaultPageProps {
    children: ReactNode
    pageName: string
}

export const DefaultPage = ({ children, pageName }: DefaultPageProps) => (
    <>
        <Header page={pageName} />
        <StyledPage>
            <BackButton />
            <StopRobotDialog />
            {children}
        </StyledPage>
    </>
)
