//    Copyright 2020 EPAM Systems, Inc.
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

import { SagaIterator } from 'redux-saga';
import { call, put, select, takeLatest } from 'redux-saga/effects';

import { Order } from 'Feature/Checkout/client/Integration/api';
import * as Commerce from 'Foundation/Commerce/client';
import { Action, Result } from 'Foundation/Integration/client';

import * as actions from './actions';
import { actionTypes } from './actionTypes';
import { OrderHistoryRequestPayload } from './models';
import * as selectors from './selectors';

const ItemsPerPage = 3;

export function* getOrderHistory(requestData: Action<OrderHistoryRequestPayload>) {
  try {
    yield put(actions.GetOrderHistoryRequest());
    const firstPageNumber = 0;
    const { data, error }: Result<Commerce.Order[]> = yield call(Order.getOrders, firstPageNumber, ItemsPerPage);

    if (error) {
      return yield put(actions.GetOrderHistoryFailure(error.message || 'Error Occured'));
    }

    const isLastPage = !data || data.length < ItemsPerPage;
    yield put(actions.GetOrderHistorySuccess(data, firstPageNumber, isLastPage));
  } catch (e) {
    yield put(actions.GetOrderHistoryFailure(e.message));
  }
}

export function* loadMoreHistory() {
  try {
    let currentPageNumber: number = yield select(selectors.orderHistoryCurrentPageNumber);
    currentPageNumber++;

    yield put(actions.GetOrderHistoryRequest());
    const { data, error }: Result<Commerce.Order[]> = yield call(Order.getOrders, currentPageNumber, ItemsPerPage);

    if (error) {
      return yield put(actions.GetOrderHistoryFailure(error.message || 'Error Occured'));
    }

    const existingOrders: Commerce.Order[] = yield select(selectors.orderHistoryList);
    const orders = [...existingOrders, ...data];
    const isLastPage = !data || data.length < ItemsPerPage;
    yield put(actions.GetOrderHistorySuccess(orders, currentPageNumber, isLastPage));
  } catch (e) {
    yield put(actions.GetOrderHistoryFailure(e.message));
  }
}

function* watch(): SagaIterator {
  yield takeLatest(actionTypes.GET_ORDER_HISTORY, getOrderHistory);
  yield takeLatest(actionTypes.ORDER_HISTORY_LOAD_MORE, loadMoreHistory);
}

export default [watch()];
