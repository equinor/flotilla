import { Icon, Typography } from '@equinor/eds-core-react'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { TextAlignedButton } from 'components/Styles/StyledComponents'
import styled from 'styled-components'

const StyledDiv = styled.div`
    flex-direction: column;
`

const StyledAlertTitle = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`

interface DockBannerProps {
    alertCategory: AlertCategory
}

export const DockBanner = ({ alertCategory }: DockBannerProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledDiv>
            <StyledAlertTitle>
                <Icon name="error_outlined" />
                <Typography>
                    {alertCategory === AlertCategory.WARNING ? TranslateText('WARNING') : TranslateText('INFO')}
                </Typography>
            </StyledAlertTitle>
            <TextAlignedButton variant="ghost" color="secondary">
                {alertCategory === AlertCategory.WARNING
                    ? TranslateText('Dock banner text')
                    : TranslateText('Dismiss dock banner text')}
            </TextAlignedButton>
        </StyledDiv>
    )
}
