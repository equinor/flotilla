import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { arrow_back } from '@equinor/eds-icons'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'

Icon.add({ arrow_back })

const StyledButton = styled(Button)`
    width: 100px;
    align-items: baseline;
`

export function BackButton() {
    return (
        <StyledButton variant="ghost" href="..">
            <Icon name="arrow_back" size={16} />
            {Text('Back')}
        </StyledButton>
    )
}
