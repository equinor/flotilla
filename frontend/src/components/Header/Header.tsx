import { config } from 'config'
import { Button, Icon, TopBar, Typography } from '@equinor/eds-core-react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import styled from 'styled-components'
import { SelectLanguage } from './LanguageSelector'
import { Icons } from 'utils/icons'
import { useAlertContext } from 'components/Contexts/AlertContext'
import { AlertBanner } from 'components/Alerts/AlertsBanner'
import { FrontPageSectionId } from 'models/FrontPageSectionId'

const StyledTopBar = styled(TopBar)`
    align-items: center;
    box-shadow: none;
`
const StyledWrapper = styled.div`
    display: flex;
    flex-direction: row;
    align-items: center;
`
const HandPointer = styled.div`
    cursor: pointer;
`
const SelectLanguageWrapper = styled.div`
    margin-left: 1rem;
`
const StyledAlertList = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    align-items: center;
    gap: 0.5rem;
`
const StyledTopBarHeader = styled.div`
    display: flex;
    flex-direction: row;
    gap: 4px;
`

export const Header = ({ page }: { page: string }) => {
    const { alerts } = useAlertContext()
    const { installationName } = useInstallationContext()

    return (
        <>
            <StyledTopBar id={FrontPageSectionId.TopBar}>
                <StyledWrapper>
                    <TopBar.Header
                        onClick={() => {
                            window.location.href = `${config.FRONTEND_BASE_ROUTE}/FrontPage`
                        }}
                    >
                        <StyledTopBarHeader>
                            <HandPointer>
                                <Typography variant="body_short_bold" color="text-primary">
                                    Flotilla
                                </Typography>
                            </HandPointer>
                            <Typography variant="body_short" color="text-primary">
                                |
                            </Typography>
                            <Typography variant="body_short" color="text-primary">
                                {' '}
                                {installationName}
                            </Typography>
                        </StyledTopBarHeader>
                    </TopBar.Header>
                </StyledWrapper>
                <TopBar.Actions>
                    <Button
                        variant="ghost_icon"
                        onClick={() => {
                            window.location.href = `${config.FRONTEND_BASE_ROUTE}/`
                        }}
                    >
                        <Icon name={Icons.Platform} size={24} title="Change Asset" />
                    </Button>
                    <SelectLanguageWrapper>{SelectLanguage()}</SelectLanguageWrapper>
                </TopBar.Actions>
            </StyledTopBar>
            {Object.entries(alerts).length > 0 && installationName && page !== 'root' && (
                <StyledAlertList>
                    {Object.entries(alerts).map(([key, value]) => (
                        <AlertBanner key={key} dismissAlert={value.dismissFunction} alertCategory={value.alertCategory}>
                            {value.content}
                        </AlertBanner>
                    ))}
                </StyledAlertList>
            )}
        </>
    )
}
