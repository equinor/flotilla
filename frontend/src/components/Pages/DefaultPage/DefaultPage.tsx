import { BackButton } from 'utils/BackButton'
import { Header } from 'components/Header/Header'
import { StyledPage } from 'components/Styles/StyledComponents'
import { ReactNode } from 'react'
import { StopRobotDialog } from '../FrontPage/MissionOverview/StopDialogs'
import { redirectIfNoInstallationSelected } from 'utils/RedirectIfNoInstallationSelected'

interface DefaultPageProps {
    children: ReactNode
    pageName: string
}

export const DefaultPage = ({ children, pageName }: DefaultPageProps) => {
    redirectIfNoInstallationSelected()
    return (
        <>
            <Header page={pageName} />
            <StyledPage>
                <BackButton />
                <StopRobotDialog />
                {children}
            </StyledPage>
        </>
    )
}
