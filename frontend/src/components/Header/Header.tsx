import { Button, Icon, TopBar } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { SelectLanguage } from 'components/Header/LanguageSelector'
import { Icons } from 'utils/icons'
import { AlertDictionaryType } from 'components/Contexts/AlertContext'
import { AlertBanner } from 'components/Alerts/AlertsBanner'
import { FrontPageSectionId } from 'models/FrontPageSectionId'
import { AlertIcon } from 'components/Header/AlertIcon'
import { useNavigate } from 'react-router-dom'
import { phone_width } from 'utils/constants'
import { Installation } from 'models/Installation'

const StyledTopBar = styled(TopBar)`
    align-items: center;
    box-shadow: none;
    border-bottom: 1px solid ${tokens.colors.ui.background__medium.hex};
    background: ${tokens.colors.ui.background__default.hex};
    height: 56px;
    padding: 0 2rem;
    @media (max-width: ${phone_width}) {
        grid-column-gap: 12px;
        padding: 0 1rem;
    }
`
const BrandName = styled.span`
    font-size: 1rem;
    font-weight: 700;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__default.hex};
    cursor: pointer;
`
const InstallationName = styled.span`
    font-size: 0.68rem;
    font-weight: 500;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__tertiary.hex};
    margin-left: 12px;
    padding-left: 12px;
    border-left: 1px solid ${tokens.colors.ui.background__medium.hex};
`
const IconStyle = styled.div`
    display: flex;
    align-items: center;
    flex-direction: row-reverse;
    gap: 0.8rem;
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
const BrandWrapper = styled.div`
    display: flex;
    align-items: center;
`

interface Props {
    alertDict?: AlertDictionaryType
    installation?: Installation
}

export const Header = ({ alertDict, installation }: Props) => {
    const navigate = useNavigate()

    return (
        <>
            <StyledTopBar id={FrontPageSectionId.TopBar}>
                <TopBar.Header onClick={() => navigate(`/${installation?.installationCode || ''}`)}>
                    <BrandWrapper>
                        <BrandName>Flotilla</BrandName>
                        {installation && <InstallationName>{installation.name}</InstallationName>}
                    </BrandWrapper>
                </TopBar.Header>
                <TopBar.Actions>
                    <IconStyle>
                        {alertDict && <AlertIcon />}
                        <Button variant="ghost_icon" onClick={() => navigate(`/`)}>
                            <Icon name={Icons.Platform} size={24} title="Change Asset" />
                        </Button>
                        <Button variant="ghost_icon" onClick={() => navigate(`/info`)}>
                            <Icon name={Icons.Info} size={24} title="Info Page" />
                        </Button>
                    </IconStyle>
                    <SelectLanguageWrapper>
                        <SelectLanguage />
                    </SelectLanguageWrapper>
                </TopBar.Actions>
            </StyledTopBar>
            {alertDict && Object.entries(alertDict).length > 0 && (
                <StyledAlertList>
                    {Object.entries(alertDict).map(([key, value]) => (
                        <AlertBanner key={key} dismissAlert={value.dismissFunction} alertCategory={value.alertCategory}>
                            {value.content}
                        </AlertBanner>
                    ))}
                </StyledAlertList>
            )}
        </>
    )
}
