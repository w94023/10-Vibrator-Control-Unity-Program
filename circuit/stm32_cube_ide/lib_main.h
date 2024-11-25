#ifndef __LIB_MAIN__
#define __LIB_MAIN__

#include "stm32f1xx_hal.h"
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "usbd_cdc_if.h"
#include "packet.h"

////////////////////////////////////////////////
//                    USB                     //
////////////////////////////////////////////////
// 수신 버퍼 크기 정의
#define RX_BUFFER_SIZE 64
// 수신 버퍼
uint8_t usb_rx_buffer[RX_BUFFER_SIZE];

////////////////////////////////////////////////
//                   UART                     //
////////////////////////////////////////////////
// 포트 설정
extern UART_HandleTypeDef huart1;
#define UART_PORT huart1
#define USART_PORT USART1
// 수신 버퍼 크기 정의
#define UART_RX_BUFFER_SIZE 14
#define UART_DATA_BUFFER_SIZE 64
// 수신 버퍼
uint8_t uart_rx_buffer[UART_RX_BUFFER_SIZE];
uint8_t uart_data_buffer[UART_DATA_BUFFER_SIZE];
size_t uart_data_count = 0;

////////////////////////////////////////////////
//                    LED                     //
////////////////////////////////////////////////
// LED toggle
#define LED1_PIN GPIO_PIN_12
int16_t led1_toggle_count = 0;
int16_t led1_toggle_interval = 500;
#define LED2_PIN GPIO_PIN_13
int16_t led2_toggle_count = 0;
int16_t led2_toggle_interval = 1;

// 송신 데이터
uint8_t tx_data[14];

// USB 수신 이벤트
void On_USB_Data_Received(uint8_t* Buf, uint32_t *Len)
{
    // LED 2 pin toggle
    HAL_GPIO_TogglePin(GPIOB, LED2_PIN);

    // 수신 데이터 버퍼로 복사
    uint8_t len = (uint8_t) *Len;
    memset(usb_rx_buffer, '\0', 64);
    memcpy(usb_rx_buffer, Buf, len);
    memset(Buf, '\0', len);

    // 패킷 분석 시작
    Analyze_Received_Packet(usb_rx_buffer, sizeof(usb_rx_buffer));
}

// UART 수신 이벤트
void HAL_UART_RxCpltCallback(UART_HandleTypeDef *huart) {
    if (huart->Instance == USART_PORT) {

        // LED 2 pin toggle
        HAL_GPIO_TogglePin(GPIOB, GPIO_PIN_13);
        
        // 수신 데이터 버퍼로 복사
        memcpy(uart_data_buffer + uart_data_count, uart_rx_buffer, UART_RX_BUFFER_SIZE);
        uart_data_count = uart_data_count + UART_RX_BUFFER_SIZE;

        // 버퍼가 가득 찼을 시 초기화 후 처음부터 다시 채움
        if (uart_data_count > UART_DATA_BUFFER_SIZE - UART_RX_BUFFER_SIZE) {
            memset(uart_data_buffer, '\0', UART_DATA_BUFFER_SIZE);
            uart_data_count = 0;
        }

        // 패킷 분석 시작
        Analyze_Received_Packet(uart_data_buffer, sizeof(uart_data_buffer));

        // 다시 수신 대기 (비동기 모드 유지)
        HAL_UART_Receive_DMA(&huart1, uart_rx_buffer, UART_RX_BUFFER_SIZE);
    }
}

void Start()
{
    // PWM 핀 동작 시작
    for (int i = 0; i < 10; i++) {
        HAL_TIM_PWM_Start(timers[i], vibrator_channels[i]);
    }

    // 송신 데이터 헤더 및 길이 설정
    tx_data[0] = 0xFF; // 헤더 1
    tx_data[1] = 0xFF; // 헤더 2
    tx_data[2] = 0x0B; // 데이터 길이 (11)

    // UART 비동기 수신 시작
    // 수신 버퍼 사이즈 만큼 데이터를 수신해야 콜백이 호출됨
    HAL_UART_Receive_DMA(&huart1, uart_rx_buffer, UART_RX_BUFFER_SIZE);
}

void Update()
{
    // 진동자 상태로 송신 데이터 생성
    memcpy(tx_data + 3, vibrator_target_intensity, 10);

    // checksum byte 생성
    tx_data[13] = get_checksum_byte(tx_data, 2, 12);
    
    // USB 송신
    CDC_Transmit_FS(tx_data, sizeof(tx_data));

    // UART 송신
    HAL_UART_Transmit(&UART_PORT, tx_data, sizeof(tx_data), HAL_MAX_DELAY);

    // LED 1 pin toggle
    led1_toggle_count = led1_toggle_count + 1;
    if (led1_toggle_count >= led1_toggle_interval) {
        led1_toggle_count = 0;
        HAL_GPIO_TogglePin(GPIOB, LED1_PIN);
    }

    HAL_Delay(1);
}


#endif // __LIB_MAIN__