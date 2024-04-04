import { Typography, Icon } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import styled from 'styled-components'

const StyledDiv = styled.div`
    display: flex;
    align-items: center;
`

export const SmallScreenInfoText = () => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledDiv id="SmallScreenInfoText">
            <Icon name={Icons.Info} />
            <Typography>{TranslateText('Small screen info text')}</Typography>
        </StyledDiv>
    )
}
